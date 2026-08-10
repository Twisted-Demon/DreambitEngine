using System;
using System.Collections.Generic;
using Dreambit.LDtk.Loaders;

namespace Dreambit.LDtk;

public static class LDtkEntityBuilderRepository
{
    private static Dictionary<string, ILDtkEntityBuilder> _entityBuilders;

    private static void EnsureRepositoryBuild()
    {
        if(_entityBuilders == null)
            _entityBuilders = new Dictionary<string, ILDtkEntityBuilder>();

        var builderTypes = ReflectionUtils.GetAllTypesAssignableFrom(
            typeof(ILDtkEntityBuilder), true);

        foreach (var builderType in builderTypes)
        {
            var instance = (ILDtkEntityBuilder)Activator.CreateInstance(builderType);
            if (instance is null) continue;
            _entityBuilders[builderType.Name] = instance;
        }
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
}
