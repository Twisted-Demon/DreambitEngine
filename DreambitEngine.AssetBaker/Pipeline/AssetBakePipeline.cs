using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DreambitEngine.AssetBaker.Abstractions;
using DreambitEngine.AssetBaker.Core;
using DreambitEngine.AssetBaker.Pipeline.Docs;
using DreambitEngine.AssetBaker.Pipeline.Textures;

namespace DreambitEngine.AssetBaker.Pipeline;

public sealed record AssetBakeRequest(
    string InputRoot,
    string OutputPak,
    string? AssetRegistryPath = null,
    string? CacheDirectory = null,
    bool RebuildAll = false,
    bool GenerateMips = false,
    bool PremultiplyAlpha = true,
    int? MaxDimension = null,
    bool MarkSrgb = true,
    string TargetPlatform = "DesktopVK",
    bool IncludeBuiltInContent = false);

public sealed record AssetBakeProgress(
    string Stage,
    string Message,
    string? RelativePath = null,
    bool CacheHit = false);

public sealed record AssetBakeResult(
    string OutputPak,
    int BakedCount,
    int CacheHitCount,
    int UnsupportedCount,
    long OutputLength,
    TimeSpan Duration);

public sealed class AssetBakePipeline
{
    public const string RuntimeRegistryLogicalPath = "__dreambit/asset-registry.jsonb";

    public static bool HasCurrentBuiltInContent(string cacheDirectory) =>
        BuiltInContentSource.IsCurrent(cacheDirectory);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public Task<AssetBakeResult> BakePakAsync(
        AssetBakeRequest request,
        IProgress<AssetBakeProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => BakePak(request, progress, cancellationToken), cancellationToken);

    public AssetBakeResult BakePak(
        AssetBakeRequest request,
        IProgress<AssetBakeProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var stopwatch = Stopwatch.StartNew();
        var inputRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(request.InputRoot));
        if (!Directory.Exists(inputRoot))
            throw new DirectoryNotFoundException($"Asset input root '{inputRoot}' does not exist.");

        var outputPak = Path.GetFullPath(request.OutputPak);
        var cache = IncrementalBakeCache.Load(request.CacheDirectory, request.RebuildAll);
        var bakerRegistry = AssetBakerRegistry.CreateDefault();
        var pak = new PakWriter();
        var bakedCount = 0;
        var cacheHitCount = 0;
        var unsupportedCount = 0;
        var optionSignature = CreateOptionSignature(request);
        var liveCacheKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var builtInEffectPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var roots = request.IncludeBuiltInContent
            ? new[]
            {
                new BakeRoot(BuiltInContentSource.DirectoryPath, "builtin", true, true),
                new BakeRoot(inputRoot, "project", false, false)
            }
            : [new BakeRoot(inputRoot, "project", false, false)];

        foreach (var bakeRoot in roots)
        {
            var producedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var files = Directory.EnumerateFiles(bakeRoot.Path, "*", new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = false,
                    AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.System,
                    ReturnSpecialDirectories = false
                })
                .OrderBy(path => Path.GetRelativePath(bakeRoot.Path, path), StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var baker = bakerRegistry.GetByExt(Path.GetExtension(file));
                if (baker is null)
                {
                    unsupportedCount++;
                    continue;
                }

                var relativePath = NormalizeRelativePath(Path.GetRelativePath(bakeRoot.Path, file));
                var expectedLogicalPath = Path.ChangeExtension(relativePath, baker.OutputExtension)
                    .Replace('\\', '/')
                    .ToLowerInvariant();
                if (!bakeRoot.IsBuiltIn && builtInEffectPaths.Contains(expectedLogicalPath))
                {
                    // Engine effects are reserved. Older projects copied these files
                    // into Assets; ignoring those stale copies lets upgraded projects
                    // receive the current embedded shader without losing custom .fx
                    // support at every other logical path.
                    progress?.Report(new AssetBakeProgress(
                        "BuiltIn",
                        $"Using the engine effect for {relativePath}",
                        relativePath));
                    continue;
                }
                var sourceHash = ComputeHash(file);
                var cacheKey = $"{bakeRoot.CachePrefix}/{relativePath}".ToLowerInvariant();
                liveCacheKeys.Add(cacheKey);
                AssetBlob blob;
                if (cache.TryRead(cacheKey, sourceHash, optionSignature, out blob))
                {
                    cacheHitCount++;
                    progress?.Report(new AssetBakeProgress(
                        "Cache",
                        $"Reused {relativePath}",
                        relativePath,
                        true));
                }
                else
                {
                    progress?.Report(new AssetBakeProgress(
                        "Bake",
                        $"Baking {relativePath}",
                        relativePath));
                    blob = baker.BakeToBytes(new BakeContext
                    {
                        InputPath = file,
                        OutputPath = string.Empty,
                        GenerateMips = request.GenerateMips,
                        PremultiplyAlpha = request.PremultiplyAlpha,
                        MaxDimension = request.MaxDimension,
                        MarkSRgb = request.MarkSrgb,
                        TargetPlatform = request.TargetPlatform,
                        LogicalRoot = bakeRoot.Path
                    });
                    cache.Write(cacheKey, sourceHash, optionSignature, blob);
                    bakedCount++;
                }

                if (!producedPaths.Add(blob.LogicalPath))
                    throw new InvalidOperationException(
                        $"Two {bakeRoot.CachePrefix} source assets produce '{blob.LogicalPath}'.");
                if (bakeRoot.IsBuiltIn && blob.Type == AssetType.Effect)
                    builtInEffectPaths.Add(blob.LogicalPath);
                if (bakeRoot.AllowProjectOverride)
                    pak.Add(blob);
                else
                    pak.AddOrReplace(blob);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.AssetRegistryPath) &&
            File.Exists(request.AssetRegistryPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            pak.Add(CreateRuntimeRegistryBlob(request.AssetRegistryPath));
        }

        cache.RemoveMissing(liveCacheKeys);
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new AssetBakeProgress("Write", $"Writing {outputPak}"));
        pak.Save(outputPak);
        cache.Save();
        if (request.IncludeBuiltInContent && !string.IsNullOrWhiteSpace(request.CacheDirectory))
            BuiltInContentSource.MarkCurrent(request.CacheDirectory);
        stopwatch.Stop();
        var length = new FileInfo(outputPak).Length;
        progress?.Report(new AssetBakeProgress(
            "Complete",
            $"Wrote {pak.Count} entries ({length:N0} bytes) in {stopwatch.Elapsed.TotalSeconds:0.00}s."));
        return new AssetBakeResult(
            outputPak,
            bakedCount,
            cacheHitCount,
            unsupportedCount,
            length,
            stopwatch.Elapsed);
    }

    private static AssetBlob CreateRuntimeRegistryBlob(string registryPath)
    {
        using var stream = File.OpenRead(registryPath);
        var source = JsonSerializer.Deserialize<SourceAssetRegistry>(stream, JsonOptions)
                     ?? throw new InvalidDataException("The Dreambit asset registry is empty.");
        var seenIds = new HashSet<Guid>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var runtimeAssets = new List<RuntimeRegistryEntry>(source.Assets.Count);
        foreach (var entry in source.Assets.OrderBy(asset => asset.Path, StringComparer.OrdinalIgnoreCase))
        {
            if (entry.Id == Guid.Empty || string.IsNullOrWhiteSpace(entry.Path))
                throw new InvalidDataException("The Dreambit asset registry contains an invalid entry.");
            var logicalName = Path.ChangeExtension(entry.Path, null)!.Replace('\\', '/');
            if (!seenIds.Add(entry.Id))
                throw new InvalidDataException($"Duplicate asset ID '{entry.Id:D}'.");
            if (!seenNames.Add(logicalName))
                throw new InvalidDataException(
                    $"Two source assets resolve to runtime name '{logicalName}'.");
            runtimeAssets.Add(new RuntimeRegistryEntry(entry.Id, logicalName));
        }

        var payload = JsonSerializer.SerializeToUtf8Bytes(
            new RuntimeAssetRegistryDocument(1, runtimeAssets),
            JsonOptions);
        using var output = new MemoryStream();
        JsnbWriter.Write(output, payload, 0);
        return new AssetBlob(
            RuntimeRegistryLogicalPath,
            AssetType.Json,
            ".jsonb",
            output.ToArray());
    }

    private static string CreateOptionSignature(AssetBakeRequest request) =>
        $"v2;mips={request.GenerateMips};premul={request.PremultiplyAlpha};" +
        $"max={request.MaxDimension?.ToString() ?? "none"};srgb={request.MarkSrgb};" +
        $"platform={request.TargetPlatform}";

    private static string ComputeHash(string path)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            128 * 1024,
            FileOptions.SequentialScan);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string NormalizeRelativePath(string path) =>
        path.Replace('\\', '/').TrimStart('/');

    private sealed record BakeRoot(
        string Path,
        string CachePrefix,
        bool AllowProjectOverride,
        bool IsBuiltIn);

    private sealed class SourceAssetRegistry
    {
        public List<SourceAssetRegistryEntry> Assets { get; set; } = [];
    }

    private sealed class SourceAssetRegistryEntry
    {
        public Guid Id { get; set; }
        public string Path { get; set; } = string.Empty;
    }

    private sealed record RuntimeAssetRegistryDocument(
        int SchemaVersion,
        IReadOnlyList<RuntimeRegistryEntry> Assets);

    private sealed record RuntimeRegistryEntry(Guid Id, string Name);

    private sealed class IncrementalBakeCache
    {
        private readonly string? _directory;
        private readonly string? _manifestPath;
        private readonly BakeCacheDocument _document;

        private IncrementalBakeCache(
            string? directory,
            BakeCacheDocument document)
        {
            _directory = directory;
            _manifestPath = directory is null ? null : Path.Combine(directory, "cache.json");
            _document = document;
        }

        public static IncrementalBakeCache Load(string? directory, bool rebuildAll)
        {
            if (string.IsNullOrWhiteSpace(directory))
                return new IncrementalBakeCache(null, new BakeCacheDocument());
            var fullDirectory = Path.GetFullPath(directory);
            if (rebuildAll)
                return new IncrementalBakeCache(fullDirectory, new BakeCacheDocument());
            var manifest = Path.Combine(fullDirectory, "cache.json");
            try
            {
                if (File.Exists(manifest))
                {
                    using var stream = File.OpenRead(manifest);
                    var document = JsonSerializer.Deserialize<BakeCacheDocument>(stream, JsonOptions);
                    if (document is not null && document.SchemaVersion == 1)
                        return new IncrementalBakeCache(fullDirectory, document);
                }
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or JsonException)
            {
            }
            return new IncrementalBakeCache(fullDirectory, new BakeCacheDocument());
        }

        public bool TryRead(
            string key,
            string sourceHash,
            string optionSignature,
            out AssetBlob blob)
        {
            blob = null!;
            if (_directory is null ||
                !_document.Entries.TryGetValue(key, out var entry) ||
                entry.SourceHash != sourceHash ||
                entry.OptionSignature != optionSignature)
            {
                return false;
            }
            var blobPath = Path.Combine(_directory, "blobs", entry.BlobFile);
            if (!File.Exists(blobPath))
                return false;
            try
            {
                blob = new AssetBlob(
                    entry.LogicalPath,
                    entry.AssetType,
                    entry.Extension,
                    File.ReadAllBytes(blobPath));
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        public void Write(
            string key,
            string sourceHash,
            string optionSignature,
            AssetBlob blob)
        {
            if (_directory is null)
                return;
            var blobDirectory = Path.Combine(_directory, "blobs");
            Directory.CreateDirectory(blobDirectory);
            var blobFile = Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes(key)))
                .ToLowerInvariant() + ".blob";
            var path = Path.Combine(blobDirectory, blobFile);
            File.WriteAllBytes(path, blob.Data);
            _document.Entries[key] = new BakeCacheEntry
            {
                SourceHash = sourceHash,
                OptionSignature = optionSignature,
                LogicalPath = blob.LogicalPath,
                AssetType = blob.Type,
                Extension = blob.Extension,
                BlobFile = blobFile
            };
        }

        public void RemoveMissing(ISet<string> liveKeys)
        {
            foreach (var key in _document.Entries.Keys.Where(key => !liveKeys.Contains(key)).ToArray())
                _document.Entries.Remove(key);
        }

        public void Save()
        {
            if (_directory is null || _manifestPath is null)
                return;
            Directory.CreateDirectory(_directory);
            var temporaryPath = _manifestPath + $".{Guid.NewGuid():N}.tmp";
            try
            {
                using (var stream = new FileStream(
                           temporaryPath,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.None))
                {
                    JsonSerializer.Serialize(stream, _document, JsonOptions);
                    stream.Flush(true);
                }
                File.Move(temporaryPath, _manifestPath, true);
            }
            finally
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }

    private sealed class BakeCacheDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, BakeCacheEntry> Entries { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class BakeCacheEntry
    {
        public string SourceHash { get; set; } = string.Empty;
        public string OptionSignature { get; set; } = string.Empty;
        public string LogicalPath { get; set; } = string.Empty;
        public AssetType AssetType { get; set; }
        public string Extension { get; set; } = string.Empty;
        public string BlobFile { get; set; } = string.Empty;
    }
}
