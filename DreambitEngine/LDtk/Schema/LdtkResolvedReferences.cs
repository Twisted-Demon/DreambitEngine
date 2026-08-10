#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dreambit.LDtk;

public partial class LDtkLevel
{
    [JsonIgnore]
    public LDtkFile Project { get; internal set; } = null!;

    [JsonIgnore]
    public string SourcePath { get; internal set; } = string.Empty;

    [JsonIgnore]
    public string? BackgroundSourcePath { get; internal set; }

    [JsonIgnore]
    public string? BackgroundAssetName { get; internal set; }
}

public partial class TilesetDefinition
{
    [JsonIgnore]
    public LDtkFile Project { get; internal set; } = null!;

    [JsonIgnore]
    public string? SourcePath { get; internal set; }

    [JsonIgnore]
    public string? AssetName { get; internal set; }
}

public partial class EnumDefinition
{
    [JsonIgnore]
    public LDtkFile Project { get; internal set; } = null!;

    [JsonIgnore]
    public string? SourcePath { get; internal set; }
}

public partial class EntityDefinition
{
    [JsonIgnore]
    public LDtkFile Project { get; internal set; } = null!;

    [JsonIgnore]
    public TilesetDefinition? Tileset => TilesetId is { } uid ? Project.GetTileset(uid) : null;
}

public partial class LayerInstance
{
    [JsonIgnore]
    public LDtkFile Project { get; internal set; } = null!;

    [JsonIgnore]
    public LDtkLevel Level { get; internal set; } = null!;

    [JsonIgnore]
    public string? TilesetSourcePath { get; internal set; }

    [JsonIgnore]
    public string? TilesetAssetName { get; internal set; }

    [JsonIgnore]
    public TilesetDefinition? Tileset => _TilesetDefUid is { } uid ? Project.GetTileset(uid) : null;
}

public partial class EntityInstance
{
    [JsonIgnore]
    public LDtkFile Project { get; internal set; } = null!;

    [JsonIgnore]
    public LDtkLevel Level { get; internal set; } = null!;

    [JsonIgnore]
    public LayerInstance Layer { get; internal set; } = null!;

    [JsonIgnore]
    public EntityDefinition Definition
    {
        get
        {
            foreach (var definition in Project.Defs.Entities ?? [])
                if (definition.Uid == DefUid)
                    return definition;

            throw new LdtkException($"No entity definition with UID '{DefUid}' exists.");
        }
    }
}

public partial class FieldInstance
{
    [JsonIgnore]
    public LDtkFile Project { get; internal set; } = null!;

    public T? GetValue<T>()
    {
        if (_Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return default;

        return _Value.Deserialize<T>(LdtkJson.Options);
    }

    public bool TryGetEntityReference(out EntityReference reference)
    {
        reference = default!;
        if (_Value.ValueKind != JsonValueKind.Object)
            return false;

        var parsed = _Value.Deserialize<EntityReference>(LdtkJson.Options);
        if (parsed is null)
            return false;

        reference = parsed;
        return true;
    }

    public IReadOnlyList<EntityReference> GetEntityReferences()
    {
        if (_Value.ValueKind != JsonValueKind.Array)
            return [];

        return _Value.Deserialize<EntityReference[]>(LdtkJson.Options) ?? [];
    }

    public EntityInstance ResolveEntityReference()
    {
        if (!TryGetEntityReference(out var reference))
            throw new LdtkException($"Field '{_Identifier}' is not a single EntityRef value.");

        return Project.ResolveEntity(reference);
    }

    public string? ResolveFilePath()
    {
        if (!_Type.Contains("FilePath", StringComparison.Ordinal) || _Value.ValueKind == JsonValueKind.Null)
            return null;

        var relativePath = _Value.GetString();
        return string.IsNullOrWhiteSpace(relativePath) ? null : Project.ResolveExternalPath(relativePath);
    }
}
