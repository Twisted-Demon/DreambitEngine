using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Reflection;
using Type = System.Type;

namespace Dreambit.ECS;

public class    ComponentRepository
{
    private static readonly ConcurrentDictionary<Type, bool> HasOnUpdateOverrideByType = [];

    private readonly HashSet<Component> _attachedComponents = [];
    private readonly HashSet<Component> _updatableComponents = [];
    private readonly HashSet<Component> _componentsToAttach = [];
    private readonly HashSet<Component> _componentsToDetach = [];
    private readonly Logger<ComponentRepository> _logger = new();

    private Scene _scene;

    public ComponentRepository(Scene scene)
    {
        _scene = scene;
    }

    public void AttachComponent<T>(T component) where T : Component
    {
        if (component == null) return;

        // If already queued to attach or already attached or pending detach, do nothing
        if (_componentsToAttach.Contains(component)) return;
        if (_attachedComponents.Contains(component)) return;
        if (_componentsToDetach.Contains(component)) return;

        _componentsToAttach.Add(component);
    }

    public void DetachComponent<T>(T component) where T : Component
    {
        if (component == null)
        {
            _logger.Warn("Could not destroy component, component is null");
            return;
        }

        // Already queued to detach?
        if (_componentsToDetach.Contains(component))
        {
            _logger.Trace("ComponentList: {0} is already being removed", component.GetType().Name);
            return;
        }

        // If it was never attached and is only pending attachment, cancel that
        if (_componentsToAttach.Contains(component))
        {
            _componentsToAttach.Remove(component);
            return;
        }

        // Queue for detach only if it is currently attached
        if (_attachedComponents.Contains(component))
            _componentsToDetach.Add(component);
    }

    internal void DestroyAllComponentsNow()
    {
        // Kill anything still pending attach (that isn't already attached)
        foreach (var c in _componentsToAttach)
        {
            if (_attachedComponents.Contains(c)) continue;
            c.RemoveFromEntity();
            c.Destroy();
            c.Entity = null;
            c.Dispose();
        }

        _componentsToAttach.Clear();

        // Kill all attached components
        foreach (var c in _attachedComponents)
        {
            if (c is DrawableComponent dc && _scene != null)
                _scene.Drawables.Remove(dc);

            c.RemoveFromEntity();
            c.Destroy();    
            c.Entity = null;
            c.Dispose();
        }

        _attachedComponents.Clear();
        _updatableComponents.Clear();
        _componentsToDetach.Clear();
    }

    public T GetComponent<T>() where T : Component
    {
        // Search attached first
        foreach (var c in _attachedComponents)
            if (c is T t)
                return t;

        // Then pending attach
        foreach (var c in _componentsToAttach)
            if (c is T t)
                return t;

        return null;
    }

    public bool ComponentOfTypeExists(Type type)
    {
        if (type == null) return false;

        foreach (var c in _attachedComponents)
            if (type.IsAssignableFrom(c.GetType()))
                return true;

        foreach (var c in _componentsToAttach)
            if (type.IsAssignableFrom(c.GetType()))
                return true;

        return false;
    }

    public Component GetComponent(Type type)
    {
        if (type == null) return null;

        if (!type.IsSubclassOf(typeof(Component)))
            return null;

        foreach (var c in _attachedComponents)
            if (c.GetType() == type)
                return c;

        foreach (var c in _componentsToAttach)
            if (c.GetType() == type)
                return c;

        return null;
    }

    public IReadOnlyCollection<Component> GetAllAttachedComponents()
    {
        // Return the backing set as a read-only view (no allocations here)
        return _attachedComponents;
    }

    public IReadOnlyCollection<Component> GetAllActiveComponents()
    {
        // Build a list of enabled components (no LINQ/ToHashSet)
        var list = new List<Component>(_attachedComponents.Count);
        foreach (var c in _attachedComponents)
            if (c.Enabled)
                list.Add(c);
        return list;
    }

    public IReadOnlyCollection<Component> GetAllComponents()
    {
        // Union of pending + attached, avoiding duplicates
        var list = new List<Component>(_componentsToAttach.Count + _attachedComponents.Count);
        var seen = new HashSet<Component>();

        foreach (var c in _componentsToAttach)
            if (seen.Add(c))
                list.Add(c);
        foreach (var c in _attachedComponents)
            if (seen.Add(c))
                list.Add(c);

        return list;
    }

    public IReadOnlyCollection<Component> GetAllComponentsToAttach()
    {
        var list = new List<Component>(_componentsToAttach.Count);
        list.AddRange(_componentsToAttach);

        return list;
    }

    public void ClearLists()
    {
        _scene = null;
        _attachedComponents.Clear();
        _updatableComponents.Clear();
        _componentsToAttach.Clear();
        _componentsToDetach.Clear();
    }

    public void UpdateComponents()
    {
        foreach (var c in _updatableComponents)
            if (c.Enabled)
                c.Update();
    }

    public void PhysicsUpdateComponents()
    {
        foreach (var c in _attachedComponents)
            if (c.Enabled)
                c.PhysicsUpdate();
    }

    public void UpdateLists()
    {
        // Handle Creation
        foreach (var add in _componentsToAttach)
            if (_attachedComponents.Add(add))
            {
                if (add is DrawableComponent dc && _scene != null)
                    _scene.Drawables.Add(dc);

                add.AddToEntity();

                if (OverridesOnUpdate(add.GetType()))
                    _updatableComponents.Add(add);
            }

        _componentsToAttach.Clear();

        // Handle Deletion
        foreach (var det in _componentsToDetach)
            if (_attachedComponents.Remove(det))
            {
                _updatableComponents.Remove(det);

                if (det is DrawableComponent dc && _scene != null)
                    _scene.Drawables.Remove(dc);

                det.RemoveFromEntity();
                det.Destroy();
                det.Entity = null;
                det.Dispose();
            }

        _componentsToDetach.Clear();
    }

    private static bool OverridesOnUpdate(Type componentType)
    {
        return HasOnUpdateOverrideByType.GetOrAdd(componentType, static type =>
        {
            var onUpdate = type.GetMethod(
                nameof(Component.OnUpdate),
                BindingFlags.Instance | BindingFlags.Public,
                null,
                Type.EmptyTypes,
                null);

            return onUpdate is not null &&
                   onUpdate.DeclaringType != typeof(Component) &&
                   onUpdate.GetBaseDefinition().DeclaringType == typeof(Component);
        });
    }
}
