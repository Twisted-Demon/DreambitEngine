#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dreambit.LDtk;

public partial class LDtkFile
{
    private readonly Dictionary<Guid, LDtkLevel> _loadedLevels = [];
    private Func<string, LDtkLevel> _externalLevelLoader = null!;
    private bool _usesLogicalAssetPaths;

    [JsonPropertyName("flags")]
    public LdtkProjectFlag[]? Flags { get; set; }

    [JsonIgnore]
    public string SourcePath { get; private set; } = string.Empty;

    [JsonIgnore]
    public IReadOnlyList<LDtkWorld> AvailableWorlds => Worlds ?? [];

    [JsonIgnore]
    public bool RequiresWorldSelection => (Worlds?.Length ?? 0) > 1;

    public static LDtkFile FromFile(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var project = LdtkJson.DeserializeProject(File.ReadAllText(fullPath));
        project.Attach(fullPath, externalPath =>
        {
            try
            {
                return LdtkJson.DeserializeLevel(File.ReadAllText(externalPath));
            }
            catch (Exception exception) when (exception is IOException or JsonException)
            {
                throw new LdtkException($"Could not load external LDtk level '{externalPath}'.", exception);
            }
        }, usesLogicalAssetPaths: false);
        return project;
    }

    public LDtkLoadedWorld LoadWorld()
    {
        if (Worlds is { Length: > 1 })
            throw new LdtkWorldSelectionRequiredException(Worlds);

        if (Worlds is { Length: 1 })
            return CreateLoadedWorld(Worlds[0]);

        return new LDtkLoadedWorld(
            this,
            identifier: "World",
            iid: Guid.Empty,
            levels: Levels ?? [],
            layout: WorldLayout,
            worldGridWidth: WorldGridWidth,
            worldGridHeight: WorldGridHeight);
    }

    public LDtkLoadedWorld LoadWorld(Guid iid)
    {
        var world = (Worlds ?? []).FirstOrDefault(candidate => candidate.Iid == iid);
        return world is null
            ? throw new LdtkException($"No LDtk world with IID '{iid}' exists in '{SourcePath}'.")
            : CreateLoadedWorld(world);
    }

    public LDtkLoadedWorld LoadWorld(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        var world = (Worlds ?? []).FirstOrDefault(candidate =>
            string.Equals(candidate.Identifier, identifier, StringComparison.Ordinal));

        return world is null
            ? throw new LdtkException($"No LDtk world named '{identifier}' exists in '{SourcePath}'.")
            : CreateLoadedWorld(world);
    }

    public TilesetDefinition GetTileset(int uid)
    {
        var tileset = (Defs?.Tilesets ?? []).FirstOrDefault(candidate => candidate.Uid == uid);
        return tileset ?? throw new LdtkException($"No LDtk tileset with UID '{uid}' exists in '{SourcePath}'.");
    }

    public string ResolveExternalPath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        return LdtkPath.Resolve(SourcePath, relativePath, _usesLogicalAssetPaths);
    }

    public string ResolveAssetName(string relativePath)
    {
        var path = ResolveExternalPath(relativePath);
        return _usesLogicalAssetPaths
            ? LdtkPath.RemoveExtension(path)
            : path;
    }

    public EntityInstance ResolveEntity(EntityReference reference)
    {
        var world = reference.WorldIid == Guid.Empty && (Worlds?.Length ?? 0) <= 1
            ? LoadWorld()
            : LoadWorld(reference.WorldIid);
        var level = world.LoadLevel(reference.LevelIid);

        foreach (var layer in level.LayerInstances ?? [])
        foreach (var entity in layer.EntityInstances ?? [])
            if (entity.Iid == reference.EntityIid)
                return entity;

        throw new LdtkException(
            $"Entity '{reference.EntityIid}' was not found in level '{reference.LevelIid}'.");
    }

    internal void Attach(
        string sourcePath,
        Func<string, LDtkLevel> externalLevelLoader,
        bool usesLogicalAssetPaths)
    {
        SourcePath = sourcePath;
        _externalLevelLoader = externalLevelLoader;
        _usesLogicalAssetPaths = usesLogicalAssetPaths;

        if (string.IsNullOrWhiteSpace(JsonVersion))
            throw new LdtkException($"'{sourcePath}' is not a valid LDtk project: jsonVersion is missing.");

        BindDefinitions();

        foreach (var level in EnumerateLevelStubs())
        {
            level.Project = this;
            level.SourcePath = string.IsNullOrWhiteSpace(level.ExternalRelPath)
                ? SourcePath
                : ResolveExternalPath(level.ExternalRelPath);

            if (level.LayerInstances is not null)
            {
                BindLevel(level);
                _loadedLevels[level.Iid] = level;
            }
        }
    }

    internal LDtkLevel LoadLevel(LDtkLevel levelStub)
    {
        if (_loadedLevels.TryGetValue(levelStub.Iid, out var loaded))
            return loaded;

        if (string.IsNullOrWhiteSpace(levelStub.ExternalRelPath))
        {
            BindLevel(levelStub);
            _loadedLevels[levelStub.Iid] = levelStub;
            return levelStub;
        }

        var path = ResolveExternalPath(levelStub.ExternalRelPath);
        var level = _externalLevelLoader(path);
        if (level.Iid != levelStub.Iid)
            throw new LdtkException(
                $"External LDtk level '{path}' has IID '{level.Iid}', but the project expected '{levelStub.Iid}'.");
        level.Project = this;
        level.SourcePath = path;
        BindLevel(level);
        _loadedLevels[level.Iid] = level;
        return level;
    }

    private LDtkLoadedWorld CreateLoadedWorld(LDtkWorld world)
    {
        return new LDtkLoadedWorld(
            this,
            world.Identifier,
            world.Iid,
            world.Levels ?? [],
            world.WorldLayout,
            world.WorldGridWidth,
            world.WorldGridHeight);
    }

    private IEnumerable<LDtkLevel> EnumerateLevelStubs()
    {
        if (Worlds is { Length: > 0 })
            return Worlds.SelectMany(world => world.Levels ?? []);

        return Levels ?? [];
    }

    private void BindDefinitions()
    {
        foreach (var tileset in Defs?.Tilesets ?? [])
        {
            tileset.Project = this;
            if (!string.IsNullOrWhiteSpace(tileset.RelPath))
            {
                tileset.SourcePath = ResolveExternalPath(tileset.RelPath);
                tileset.AssetName = ResolveAssetName(tileset.RelPath);
            }
        }

        foreach (var externalEnum in Defs?.ExternalEnums ?? [])
        {
            externalEnum.Project = this;
            if (!string.IsNullOrWhiteSpace(externalEnum.ExternalRelPath))
                externalEnum.SourcePath = ResolveExternalPath(externalEnum.ExternalRelPath);
        }

        foreach (var entity in Defs?.Entities ?? [])
            entity.Project = this;
    }

    private void BindLevel(LDtkLevel level)
    {
        level.Project = this;

        if (!string.IsNullOrWhiteSpace(level.BgRelPath))
        {
            level.BackgroundSourcePath = ResolveExternalPath(level.BgRelPath);
            level.BackgroundAssetName = ResolveAssetName(level.BgRelPath);
        }

        foreach (var field in level.FieldInstances ?? [])
            field.Project = this;

        foreach (var layer in level.LayerInstances ?? [])
        {
            layer.Project = this;
            if (!string.IsNullOrWhiteSpace(layer._TilesetRelPath))
            {
                layer.TilesetSourcePath = ResolveExternalPath(layer._TilesetRelPath);
                layer.TilesetAssetName = ResolveAssetName(layer._TilesetRelPath);
            }

            foreach (var entity in layer.EntityInstances ?? [])
            {
                entity.Project = this;
                foreach (var field in entity.FieldInstances ?? [])
                    field.Project = this;
            }
        }
    }
}

public sealed class LDtkLoadedWorld
{
    private readonly LDtkFile _project;

    internal LDtkLoadedWorld(
        LDtkFile project,
        string identifier,
        Guid iid,
        IReadOnlyList<LDtkLevel> levels,
        WorldLayout? layout,
        int? worldGridWidth,
        int? worldGridHeight)
    {
        _project = project;
        Identifier = identifier;
        Iid = iid;
        Levels = levels;
        Layout = layout;
        WorldGridWidth = worldGridWidth;
        WorldGridHeight = worldGridHeight;
    }

    public string Identifier { get; }
    public Guid Iid { get; }
    public IReadOnlyList<LDtkLevel> Levels { get; }
    public WorldLayout? Layout { get; }
    public int? WorldGridWidth { get; }
    public int? WorldGridHeight { get; }

    public LDtkLevel LoadLevel(Guid iid)
    {
        var level = Levels.FirstOrDefault(candidate => candidate.Iid == iid);
        return level is null
            ? throw new LdtkException($"No level with IID '{iid}' exists in world '{Identifier}'.")
            : _project.LoadLevel(level);
    }

    public LDtkLevel LoadLevel(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        var level = Levels.FirstOrDefault(candidate =>
            string.Equals(candidate.Identifier, identifier, StringComparison.Ordinal));

        return level is null
            ? throw new LdtkException($"No level named '{identifier}' exists in world '{Identifier}'.")
            : _project.LoadLevel(level);
    }

    public IEnumerable<LDtkLevel> LoadLevels(IEnumerable<Guid> iids)
    {
        foreach (var iid in iids)
            yield return LoadLevel(iid);
    }
}

internal static class LdtkPath
{
    public static string Resolve(string sourcePath, string relativePath, bool logical)
    {
        if (!logical)
        {
            var directory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
            return Path.GetFullPath(Path.Combine(directory, relativePath));
        }

        var baseDirectory = Path.GetDirectoryName(sourcePath.Replace('/', Path.DirectorySeparatorChar))
                            ?? string.Empty;
        var combined = Path.Combine(baseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var segments = new List<string>();

        foreach (var segment in combined.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
                continue;
            if (segment == "..")
            {
                if (segments.Count == 0)
                    throw new LdtkException($"External path '{relativePath}' escapes the content root.");
                segments.RemoveAt(segments.Count - 1);
                continue;
            }

            segments.Add(segment);
        }

        return string.Join('/', segments).ToLowerInvariant();
    }

    public static string RemoveExtension(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Length == 0 ? path : path[..^extension.Length];
    }
}
