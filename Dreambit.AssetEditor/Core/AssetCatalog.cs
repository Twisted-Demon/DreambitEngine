using System.Reflection;
using System.Runtime.Loader;
using Dreambit;
using Dreambit.ECS;

namespace Dreambit.AssetEditor.Core;

internal sealed class AssetCatalog
{
    private readonly HashSet<string> _pluginDirectories = new(StringComparer.OrdinalIgnoreCase);

    public AssetCatalog()
    {
        AssemblyLoadContext.Default.Resolving += ResolveDependency;
        Refresh();
    }

    public IReadOnlyList<Type> AssetTypes { get; private set; } = [];
    public IReadOnlyList<Type> ComponentTypes { get; private set; } = [];
    public IReadOnlyList<Assembly> ExternalAssemblies { get; private set; } = [];

    public Assembly LoadExternalAssembly(string path)
    {
        var fullPath = Path.GetFullPath(path);
        _pluginDirectories.Add(Path.GetDirectoryName(fullPath)!);

        var existing = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => string.Equals(a.Location, fullPath, StringComparison.OrdinalIgnoreCase));
        var assembly = existing ?? AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);

        DreambitJson.RefreshConverters();
        BlueprintResolver.RebuildComponentTypeRegistry();
        Refresh();
        return assembly;
    }

    public void Refresh()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).ToArray();
        var jsonBacked = new HashSet<Type>();

        foreach (var assembly in assemblies)
        foreach (var type in GetLoadableTypes(assembly))
        {
            if (type.IsAbstract || type.IsInterface || !typeof(IAssetLoader).IsAssignableFrom(type))
                continue;

            try
            {
                if (Activator.CreateInstance(type) is IAssetLoader loader &&
                    loader.Extension.Equals(".jsonb", StringComparison.OrdinalIgnoreCase) &&
                    typeof(DreambitAsset).IsAssignableFrom(loader.TargetType))
                    jsonBacked.Add(loader.TargetType);
            }
            catch
            {
                // Loader constructors are expected to be cheap and parameterless.
                // A loader that cannot be instantiated cannot advertise editor support.
            }
        }

        AssetTypes = jsonBacked
            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
            .OrderBy(t => t.Name)
            .ToArray();

        ComponentTypes = assemblies
            .SelectMany(GetLoadableTypes)
            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && typeof(Component).IsAssignableFrom(t))
            .OrderBy(t => t.Name)
            .ToArray();

        ExternalAssemblies = assemblies
            .Where(a => a != typeof(DreambitAsset).Assembly &&
                        a.GetReferencedAssemblies().Any(r => r.Name == typeof(DreambitAsset).Assembly.GetName().Name))
            .OrderBy(a => a.GetName().Name)
            .ToArray();
    }

    private Assembly? ResolveDependency(AssemblyLoadContext context, AssemblyName name)
    {
        foreach (var directory in _pluginDirectories)
        {
            var candidate = Path.Combine(directory, name.Name + ".dll");
            if (File.Exists(candidate))
                return context.LoadFromAssemblyPath(candidate);
        }
        return null;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
        catch
        {
            return [];
        }
    }
}
