using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dreambit.LDtk.Loaders;

namespace Dreambit.LDtk;

public static class LDtkEntityBuilderRepository
{
    private static Dictionary<string, ILDtkEntityBuilder> _entityBuilders;

    private static void EnsureRepositoryBuild()
    {
        if (_entityBuilders is not null)
            return;

        var entityBuilders = new Dictionary<string, ILDtkEntityBuilder>(StringComparer.Ordinal);

        var builderTypes = ReflectionUtils.GetAllTypesAssignableFrom(
            typeof(ILDtkEntityBuilder), true);

        foreach (var builderType in builderTypes)
        {
            var instance = (ILDtkEntityBuilder)Activator.CreateInstance(builderType);
            if (instance is null) continue;

            var identifiers = instance.EntityDefinitionIdentifiers;
            if (identifiers is null || identifiers.Length == 0)
                throw new InvalidOperationException(
                    $"LDtk entity builder '{builderType.FullName}' must declare at least one identifier.");

            foreach (var identifier in identifiers)
            {
                if (string.IsNullOrWhiteSpace(identifier))
                    throw new InvalidOperationException(
                        $"LDtk entity builder '{builderType.FullName}' contains an empty identifier.");

                if (!entityBuilders.TryAdd(identifier, instance))
                {
                    var registeredType = entityBuilders[identifier].GetType();
                    if (registeredType == builderType)
                        continue;

                    throw new InvalidOperationException(
                        $"LDtk entity identifier '{identifier}' is registered by both " +
                        $"'{registeredType.FullName}' and '{builderType.FullName}'.");
                }
            }
        }

        _entityBuilders = entityBuilders;
    }

    public static ILDtkEntityBuilder GetEntityBuilder(string entityIdentifier)
    {
        EnsureRepositoryBuild();
        return _entityBuilders.GetValueOrDefault(entityIdentifier);
    }

    public static bool TryGetEntityBuilder(string entityIdentifier, out  ILDtkEntityBuilder builder)
    {
        EnsureRepositoryBuild();

        return _entityBuilders.TryGetValue(entityIdentifier, out builder);
    }

    internal static void ReleaseAssembly(Assembly assembly)
    {
        if (_entityBuilders is null)
            return;

        foreach (var key in _entityBuilders
                     .Where(pair => pair.Value.GetType().Assembly == assembly)
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            _entityBuilders.Remove(key);
        }
    }

    internal static void Refresh() => _entityBuilders = null;
}
