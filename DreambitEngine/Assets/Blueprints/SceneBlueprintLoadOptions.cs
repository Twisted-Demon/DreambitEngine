namespace Dreambit;

/// <summary>Controls how a serialized scene is materialized into a live scene.</summary>
public sealed class SceneBlueprintLoadOptions
{
    public static SceneBlueprintLoadOptions Runtime { get; } = new();

    public static SceneBlueprintLoadOptions Editor { get; } = new()
    {
        AllowMissingComponentTypes = true,
        PreserveEntityIds = true,
        TolerateComponentLoadErrors = true
    };

    /// <summary>
    /// Keeps entities and their serialized component payloads loadable when a component
    /// assembly is temporarily unavailable. Missing components are omitted from the live ECS.
    /// </summary>
    public bool AllowMissingComponentTypes { get; init; }

    /// <summary>Uses serialized entity GUIDs as live entity IDs.</summary>
    public bool PreserveEntityIds { get; init; } = true;

    /// <summary>
    /// Keeps an editor scene open when a known component has stale members, invalid
    /// references, or cannot currently be constructed. Runtime loading remains strict.
    /// </summary>
    public bool TolerateComponentLoadErrors { get; init; }
}
