using System.Reflection;
using System.Runtime.Loader;
using Dreambit;

namespace Dreambit.Editor.Compilation;

internal sealed record LoadedGameAssembly(
    Assembly Assembly,
    GameTypeCatalog Types,
    string SourceAssemblyPath,
    string ShadowDirectory,
    int Generation);

internal sealed class GameAssemblyLoadService : IDisposable
{
    private readonly string _shadowRoot;
    private readonly Action<GameCodeMessage>? _report;
    private CollectibleGameLoadContext? _loadContext;
    private LoadedGameAssembly? _current;
    private int _generation;
    private bool _disposed;

    public GameAssemblyLoadService(
        string projectRoot,
        Action<GameCodeMessage>? report = null)
    {
        _shadowRoot = Path.Combine(projectRoot, ".dreambit", "cache", "assemblies");
        _report = report;
    }

    public LoadedGameAssembly? Current => _current;
    public event Action<LoadedGameAssembly?>? Reloading;
    public event Action<LoadedGameAssembly>? Reloaded;

    public bool TryLoad(string assemblyPath, out string? error)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var sourcePath = Path.GetFullPath(assemblyPath);
        if (!File.Exists(sourcePath))
        {
            error = $"Built game assembly '{sourcePath}' does not exist.";
            return false;
        }

        CollectibleGameLoadContext? candidateContext = null;
        string? candidateShadowDirectory = null;
        try
        {
            candidateShadowDirectory = CreateShadowCopy(sourcePath, ++_generation);
            var shadowAssemblyPath = Path.Combine(
                candidateShadowDirectory,
                Path.GetFileName(sourcePath));
            candidateContext = new CollectibleGameLoadContext(shadowAssemblyPath);
            var assembly = candidateContext.LoadFromAssemblyPath(shadowAssemblyPath);
            var catalog = GameTypeCatalog.Discover(assembly);
            var loaded = new LoadedGameAssembly(
                assembly,
                catalog,
                sourcePath,
                candidateShadowDirectory,
                _generation);

            Reloading?.Invoke(_current);
            UnloadCurrent();
            _loadContext = candidateContext;
            _current = loaded;
            candidateContext = null;
            DreambitAssemblyCaches.Refresh();
            Reloaded?.Invoke(loaded);
            _report?.Invoke(new GameCodeMessage(
                GameCodeMessageSeverity.Information,
                $"Loaded game assembly generation {_generation}: " +
                $"{catalog.ComponentTypes.Count} components, {catalog.AssetTypes.Count} custom assets."));
            error = null;
            return true;
        }
        catch (Exception exception)
        {
            if (candidateContext is not null)
            {
                var weakReference = new WeakReference(candidateContext);
                candidateContext.Unload();
                candidateContext = null;
                for (var attempt = 0; attempt < 3 && weakReference.IsAlive; attempt++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
            }
            if (candidateShadowDirectory is not null)
                TryDeleteDirectory(candidateShadowDirectory);
            error = $"Could not load game assembly. {exception.Message}";
            _report?.Invoke(new GameCodeMessage(
                GameCodeMessageSeverity.Error,
                error,
                exception));
            return false;
        }
    }

    private string CreateShadowCopy(string assemblyPath, int generation)
    {
        var sourceDirectory = Path.GetDirectoryName(assemblyPath)!;
        var shadowDirectory = Path.Combine(
            _shadowRoot,
            $"generation-{generation:D4}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(shadowDirectory);
        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.TopDirectoryOnly))
            File.Copy(file, Path.Combine(shadowDirectory, Path.GetFileName(file)), true);
        foreach (var directory in Directory.EnumerateDirectories(sourceDirectory))
            CopyDirectory(directory, Path.Combine(shadowDirectory, Path.GetFileName(directory)));
        return shadowDirectory;
    }

    private void UnloadCurrent()
    {
        if (_loadContext is null || _current is null)
            return;

        var assembly = _current.Assembly;
        var shadowDirectory = _current.ShadowDirectory;
        var context = _loadContext;
        var weakReference = new WeakReference(context, trackResurrection: false);
        _current = null;
        _loadContext = null;
        DreambitAssemblyCaches.Release(assembly);
        context.Unload();
        assembly = null!;
        context = null!;

        for (var attempt = 0; attempt < 3 && weakReference.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        if (weakReference.IsAlive)
        {
            _report?.Invoke(new GameCodeMessage(
                GameCodeMessageSeverity.Warning,
                "The previous game assembly is still referenced after unload. " +
                "The shadow copy was retained for diagnostics."));
        }
        else
        {
            TryDeleteDirectory(shadowDirectory);
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        foreach (var child in Directory.EnumerateDirectories(source))
            CopyDirectory(child, Path.Combine(destination, Path.GetFileName(child)));
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        Reloading?.Invoke(_current);
        UnloadCurrent();
        _disposed = true;
    }

    private sealed class CollectibleGameLoadContext : AssemblyLoadContext
    {
        private static readonly HashSet<string> SharedAssemblyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            typeof(DreambitAsset).Assembly.GetName().Name!,
            typeof(Microsoft.Xna.Framework.Vector2).Assembly.GetName().Name!,
            typeof(Dreambit.EditorApi.IDreambitCustomEditor).Assembly.GetName().Name!,
            typeof(ImGuiNET.ImGui).Assembly.GetName().Name!
        };

        private readonly AssemblyDependencyResolver _resolver;

        public CollectibleGameLoadContext(string mainAssemblyPath)
            : base($"Dreambit.Game.{Guid.NewGuid():N}", isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name is not null && SharedAssemblyNames.Contains(assemblyName.Name))
                return Default.Assemblies.FirstOrDefault(assembly =>
                    AssemblyName.ReferenceMatchesDefinition(assembly.GetName(), assemblyName));

            var path = _resolver.ResolveAssemblyToPath(assemblyName);
            return path is null ? null : LoadFromAssemblyPath(path);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            return path is null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
        }
    }
}
