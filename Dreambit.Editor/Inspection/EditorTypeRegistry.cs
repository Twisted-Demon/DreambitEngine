using Dreambit.ECS;
using Dreambit.Editor.Compilation;

namespace Dreambit.Editor.Inspection;

internal sealed class EditorTypeRegistry : IDisposable
{
    private readonly GameAssemblyLoadService _assemblies;
    private readonly InspectorMetadataCache _metadata;
    private IReadOnlyList<Type> _componentTypes = [];
    private IReadOnlyList<Type> _assetTypes = [];
    private bool _disposed;

    public EditorTypeRegistry(GameAssemblyLoadService assemblies, InspectorMetadataCache metadata)
    {
        _assemblies = assemblies;
        _metadata = metadata;
        _assemblies.Reloading += OnReloading;
        _assemblies.Reloaded += OnReloaded;
        Rebuild();
    }

    public IReadOnlyList<Type> ComponentTypes => _componentTypes;
    public IReadOnlyList<Type> AssetTypes => _assetTypes;
    public event Action? Changed;

    private void OnReloading(LoadedGameAssembly? assembly)
    {
        if (assembly is not null)
            _metadata.ReleaseAssembly(assembly.Assembly);
        _componentTypes = _componentTypes.Where(type => type.Assembly != assembly?.Assembly).ToArray();
        _assetTypes = _assetTypes.Where(type => type.Assembly != assembly?.Assembly).ToArray();
    }

    private void OnReloaded(LoadedGameAssembly _) => Rebuild();

    private void Rebuild()
    {
        var gameTypes = _assemblies.Current?.Types;
        _componentTypes = GetLoadableTypes(typeof(Component).Assembly)
            .Where(IsConcreteComponent)
            .Concat(gameTypes?.ComponentTypes ?? [])
            .Distinct()
            .OrderBy(type => type.Name)
            .ToArray();
        _assetTypes = GetLoadableTypes(typeof(DreambitAsset).Assembly)
            .Where(IsConcreteAsset)
            .Concat(gameTypes?.AssetTypes ?? [])
            .Distinct()
            .OrderBy(type => type.Name)
            .ToArray();
        Changed?.Invoke();
    }

    private static bool IsConcreteComponent(Type type) =>
        typeof(Component).IsAssignableFrom(type) && !type.IsAbstract && !type.IsGenericType &&
        type.GetConstructor(Type.EmptyTypes) is not null;

    private static bool IsConcreteAsset(Type type) =>
        typeof(DreambitAsset).IsAssignableFrom(type) && !type.IsAbstract && !type.IsGenericType &&
        type.GetConstructor(Type.EmptyTypes) is not null;

    private static IEnumerable<Type> GetLoadableTypes(System.Reflection.Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (System.Reflection.ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _assemblies.Reloading -= OnReloading;
        _assemblies.Reloaded -= OnReloaded;
        _componentTypes = [];
        _assetTypes = [];
        _metadata.Clear();
        _disposed = true;
    }
}
