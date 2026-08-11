using System;
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
        PropertyConverterRegistry.ReleaseAssembly(assembly);
        BlueprintResolver.ReleaseAssembly(assembly);
        ComponentRepository.ReleaseAssembly(assembly);
        LDtkEntityBuilderRepository.ReleaseAssembly(assembly);
        Resources.ReleaseAssembly(assembly);
    }

    public static void Refresh()
    {
        DreambitJson.RefreshConverters();
        BlueprintResolver.RebuildComponentTypeRegistry();
        LDtkEntityBuilderRepository.Refresh();
        Resources.RefreshLoaders();
    }
}
