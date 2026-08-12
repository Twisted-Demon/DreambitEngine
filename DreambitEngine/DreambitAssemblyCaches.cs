using System;
using System.Collections.Generic;
using System.Reflection;
using Dreambit.ECS;
using Dreambit.LDtk;

namespace Dreambit;

/// <summary>
/// Releases engine-owned reflection caches before an editor unloads a collectible game assembly.
/// Runtime games normally never need to call this API.
/// </summary>
public static class DreambitAssemblyCaches
{
    public static void Release(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        DreambitAssetTypeRegistry.ReleaseAssembly(assembly);
        PropertyConverterRegistry.ReleaseAssembly(assembly);
        BlueprintResolver.ReleaseAssembly(assembly);
        ComponentRepository.ReleaseAssembly(assembly);
        LDtkEntityBuilderRepository.ReleaseAssembly(assembly);
        Resources.ReleaseAssembly(assembly);
    }

    public static void Refresh()
    {
        DreambitAssetTypeRegistry.Refresh();
        DreambitJson.RefreshConverters();
        BlueprintResolver.RebuildComponentTypeRegistry();
        LDtkEntityBuilderRepository.Refresh();
        Resources.RefreshLoaders();
    }

    /// <summary>
    /// Refreshes reflection caches using an explicit set of non-engine asset types. Editors use
    /// this overload to avoid rediscovering an assembly that is leaving a collectible context.
    /// </summary>
    public static void Refresh(IEnumerable<Type> assetTypes)
    {
        ArgumentNullException.ThrowIfNull(assetTypes);
        DreambitAssetTypeRegistry.Refresh(assetTypes);
        DreambitJson.RefreshConverters();
        BlueprintResolver.RebuildComponentTypeRegistry();
        LDtkEntityBuilderRepository.Refresh();
        Resources.RefreshLoaders();
    }
}
