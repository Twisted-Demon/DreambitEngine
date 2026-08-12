using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Dreambit;

/// <summary>
/// Resolves durable Dreambit asset type IDs to the currently loaded CLR types.
/// </summary>
public static class DreambitAssetTypeRegistry
{
    public const string MetadataPropertyName = "$dreambitType";

    private static readonly object Sync = new();
    private static Dictionary<string, Type> _typesById =
        new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<Type, string> _currentIdsByType = [];
    private static bool _built;

    /// <summary>
    /// Gets the canonical persisted ID for an asset type. Unannotated types fall back to their
    /// CLR full name and are therefore rename-unsafe.
    /// </summary>
    public static string GetTypeId(Type assetType)
    {
        ValidateAssetType(assetType);
        lock (Sync)
            if (_built && _currentIdsByType.TryGetValue(assetType, out var registeredId))
                return registeredId;

        return GetDeclaredTypeId(assetType);
    }

    /// <summary>Returns whether the type declares a stable, rename-safe asset type ID.</summary>
    public static bool HasStableTypeId(Type assetType)
    {
        ValidateAssetType(assetType);
        return assetType.GetCustomAttribute<DreambitAssetTypeAttribute>(false) is not null;
    }

    /// <summary>
    /// Returns whether generic JSON for this type should carry <c>$dreambitType</c> metadata.
    /// Engine-owned semantic formats retain their existing suffix-based identity.
    /// </summary>
    public static bool ShouldPersistTypeMetadata(Type assetType)
    {
        ValidateAssetType(assetType);
        return assetType.Assembly != typeof(DreambitAsset).Assembly;
    }

    public static bool TryResolve(string typeId, out Type assetType)
    {
        assetType = null;
        if (string.IsNullOrWhiteSpace(typeId))
            return false;

        EnsureBuilt();
        lock (Sync)
            return _typesById.TryGetValue(typeId.Trim(), out assetType);
    }

    public static Type Resolve(string typeId)
    {
        if (TryResolve(typeId, out var assetType))
            return assetType!;

        throw new KeyNotFoundException(
            $"No loaded Dreambit asset type claims the ID '{typeId}'. " +
            "The game assembly may be unavailable, the asset class may have been removed, or " +
            "the ID may have changed without being listed as a former ID.");
    }

    /// <summary>Rebuilds the registry from every currently loaded assembly.</summary>
    public static void Refresh()
    {
        Replace(BuildRegistry(
            AppDomain.CurrentDomain.GetAssemblies()
                // Collectible hosts own their active-generation lifecycle and use the explicit
                // overload below. Ignoring collectible contexts here prevents unloaded-but-not-yet-
                // collected generations from being rediscovered by a fallback scan.
                .Where(assembly =>
                    AssemblyLoadContext.GetLoadContext(assembly)?.IsCollectible != true)
                .SelectMany(GetLoadableAssetTypes)));
    }

    /// <summary>
    /// Rebuilds the registry from engine assets and a known set of additional asset types.
    /// Editors use this overload so an unloading collectible assembly is never rediscovered.
    /// </summary>
    public static void Refresh(IEnumerable<Type> additionalAssetTypes)
    {
        ArgumentNullException.ThrowIfNull(additionalAssetTypes);
        Replace(BuildRegistry(
            GetLoadableAssetTypes(typeof(DreambitAsset).Assembly)
                .Concat(additionalAssetTypes)));
    }

    /// <summary>
    /// Validates engine assets plus the supplied types without changing the active registry.
    /// </summary>
    public static void Validate(IEnumerable<Type> additionalAssetTypes)
    {
        ArgumentNullException.ThrowIfNull(additionalAssetTypes);
        _ = BuildRegistry(
            GetLoadableAssetTypes(typeof(DreambitAsset).Assembly)
                .Concat(additionalAssetTypes));
    }

    internal static void ReleaseAssembly(Assembly assembly)
    {
        lock (Sync)
        {
            _typesById = _typesById
                .Where(pair => pair.Value.Assembly != assembly)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
            _currentIdsByType = _currentIdsByType
                .Where(pair => pair.Key.Assembly != assembly)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            // A later lookup outside the editor's explicit refresh lifecycle should rescan after
            // the collectible context has been unloaded rather than treating this partial map as final.
            _built = false;
        }
    }

    private static void EnsureBuilt()
    {
        lock (Sync)
            if (_built)
                return;

        Refresh();
    }

    private static void Replace(RegistryState state)
    {
        lock (Sync)
        {
            _typesById = state.TypesById;
            _currentIdsByType = state.CurrentIdsByType;
            _built = true;
        }
    }

    private static RegistryState BuildRegistry(IEnumerable<Type> assetTypes)
    {
        var validTypes = assetTypes
            .Where(IsRegistrableAssetType)
            .Distinct()
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ThenBy(type => type.Assembly.GetName().Name, StringComparer.Ordinal)
            .ToArray();

        var claims = new Dictionary<string, List<Type>>(StringComparer.OrdinalIgnoreCase);
        var currentIds = new Dictionary<Type, string>();
        foreach (var type in validTypes)
        {
            var currentId = GetDeclaredTypeId(type);
            currentIds.Add(type, currentId);
            AddClaim(claims, currentId, type);

            var attribute = type.GetCustomAttribute<DreambitAssetTypeAttribute>(false);
            if (attribute is null)
                continue;

            foreach (var formerId in attribute.FormerIds)
                AddClaim(claims, formerId, type);

            // Accept the current CLR full name as a migration bridge for files created before
            // stable IDs were available. Earlier CLR names must be supplied explicitly as former IDs.
            if (!string.Equals(type.FullName, currentId, StringComparison.OrdinalIgnoreCase))
                AddClaim(claims, type.FullName!, type);
        }

        var conflicts = claims
            .Select(pair => new
            {
                pair.Key,
                Types = pair.Value.Distinct().OrderBy(type => type.FullName, StringComparer.Ordinal).ToArray()
            })
            .Where(claim => claim.Types.Length > 1)
            .OrderBy(claim => claim.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (conflicts.Length > 0)
        {
            var details = string.Join(
                Environment.NewLine,
                conflicts.Select(conflict =>
                    $"Dreambit asset type ID '{conflict.Key}' is claimed by " +
                    string.Join(", ", conflict.Types.Select(type => $"'{type.FullName}'")) + "."));
            throw new InvalidOperationException(
                "Dreambit asset type IDs must be unique across current IDs, former IDs, and " +
                $"legacy CLR identities.{Environment.NewLine}{details}");
        }

        return new RegistryState(
            claims.ToDictionary(
                pair => pair.Key,
                pair => pair.Value[0],
                StringComparer.OrdinalIgnoreCase),
            currentIds);
    }

    private static void AddClaim(
        IDictionary<string, List<Type>> claims,
        string identity,
        Type type)
    {
        if (!claims.TryGetValue(identity, out var types))
        {
            types = [];
            claims.Add(identity, types);
        }

        if (!types.Contains(type))
            types.Add(type);
    }

    private static string GetDeclaredTypeId(Type assetType)
    {
        return assetType.GetCustomAttribute<DreambitAssetTypeAttribute>(false)?.Id
               ?? assetType.FullName
               ?? throw new InvalidOperationException(
                   $"Dreambit asset type '{assetType}' does not have a CLR full name. " +
                   "Declare [DreambitAssetType] with a stable ID.");
    }

    private static void ValidateAssetType(Type assetType)
    {
        ArgumentNullException.ThrowIfNull(assetType);
        if (!typeof(DreambitAsset).IsAssignableFrom(assetType))
            throw new ArgumentException(
                $"'{assetType.FullName}' is not a DreambitAsset type.",
                nameof(assetType));
    }

    private static bool IsRegistrableAssetType(Type type)
    {
        return typeof(DreambitAsset).IsAssignableFrom(type) &&
               type != typeof(DreambitAsset) &&
               !type.IsAbstract &&
               !type.ContainsGenericParameters;
    }

    private static IEnumerable<Type> GetLoadableAssetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes().Where(IsRegistrableAssetType);
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>().Where(IsRegistrableAssetType);
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }

    private sealed record RegistryState(
        Dictionary<string, Type> TypesById,
        Dictionary<Type, string> CurrentIdsByType);
}
