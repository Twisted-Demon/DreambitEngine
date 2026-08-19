using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Type = System.Type;

namespace Dreambit.ECS;

public class ComponentRepository
{
    private static readonly ConcurrentDictionary<Type, bool>
        HasOnUpdateOverrideByType = [];

    private static readonly ConcurrentDictionary<Type, bool>
        HasOnPhysicsUpdateOverrideByType = [];

    private readonly HashSet<Component> _attachedComponents = [];
    private readonly HashSet<Component> _componentsToAttach = [];
    private readonly HashSet<Component> _componentsToDetach = [];

    private readonly HashSet<Component> _updatableComponents = [];
    private readonly HashSet<Component> _physicsUpdatableComponents = [];

    private readonly Logger<ComponentRepository> _logger = new();

    private Scene _scene;

    public ComponentRepository(
        Scene scene)
    {
        _scene = scene;
    }

    public void AttachComponent<T>(
        T component)
        where T : Component
    {
        if (component == null)
            return;

        if (_componentsToAttach.Contains(component))
            return;

        if (_attachedComponents.Contains(component))
            return;

        if (_componentsToDetach.Contains(component))
            return;

        _componentsToAttach.Add(component);
    }

    public void DetachComponent<T>(
        T component)
        where T : Component
    {
        if (component == null)
        {
            _logger.Warn(
                "Could not destroy component, component is null");

            return;
        }

        if (_componentsToDetach.Contains(component))
        {
            _logger.Trace(
                "ComponentList: {0} is already being removed",
                component.GetType().Name);

            return;
        }

        if (_componentsToAttach.Contains(component))
        {
            _componentsToAttach.Remove(component);
            return;
        }

        if (_attachedComponents.Contains(component))
            _componentsToDetach.Add(component);
    }

    internal void DestroyAllComponentsNow()
    {
        foreach (var component in _componentsToAttach)
        {
            if (_attachedComponents.Contains(component))
                continue;

            component.RemoveFromEntity();
            component.Destroy();

            component.Entity = null;

            component.Dispose();
        }

        _componentsToAttach.Clear();

        foreach (var component in _attachedComponents)
        {
            if (component is DrawableComponent drawable &&
                _scene != null)
            {
                _scene.Drawables.Remove(drawable);
            }

            component.RemoveFromEntity();
            component.Destroy();

            component.Entity = null;

            component.Dispose();
        }

        _attachedComponents.Clear();
        _updatableComponents.Clear();
        _physicsUpdatableComponents.Clear();
        _componentsToDetach.Clear();
    }

    public T GetComponent<T>()
        where T : Component
    {
        foreach (var component in _attachedComponents)
        {
            if (component is T typed)
                return typed;
        }

        foreach (var component in _componentsToAttach)
        {
            if (component is T typed)
                return typed;
        }

        return null;
    }

    public bool ComponentOfTypeExists(
        Type type)
    {
        if (type == null)
            return false;

        foreach (var component in _attachedComponents)
        {
            if (type.IsAssignableFrom(
                    component.GetType()))
            {
                return true;
            }
        }

        foreach (var component in _componentsToAttach)
        {
            if (type.IsAssignableFrom(
                    component.GetType()))
            {
                return true;
            }
        }

        return false;
    }

    public Component GetComponent(
        Type type)
    {
        if (type == null)
            return null;

        if (!typeof(Component)
                .IsAssignableFrom(type))
        {
            return null;
        }

        foreach (var component in _attachedComponents)
        {
            if (component.GetType() == type)
                return component;
        }

        foreach (var component in _componentsToAttach)
        {
            if (component.GetType() == type)
                return component;
        }

        foreach (var component in _attachedComponents)
        {
            if (type.IsAssignableFrom(
                    component.GetType()))
            {
                return component;
            }
        }

        foreach (var component in _componentsToAttach)
        {
            if (type.IsAssignableFrom(
                    component.GetType()))
            {
                return component;
            }
        }

        return null;
    }

    public IReadOnlyCollection<Component>
        GetAllAttachedComponents()
    {
        return _attachedComponents;
    }

    public IReadOnlyCollection<Component>
        GetAllActiveComponents()
    {
        var list =
            new List<Component>(
                _attachedComponents.Count);

        foreach (var component in _attachedComponents)
        {
            if (component.Enabled)
                list.Add(component);
        }

        return list;
    }

    public IReadOnlyCollection<Component>
        GetAllComponents()
    {
        var list =
            new List<Component>(
                _componentsToAttach.Count +
                _attachedComponents.Count);

        var seen =
            new HashSet<Component>();

        foreach (var component in _componentsToAttach)
        {
            if (seen.Add(component))
                list.Add(component);
        }

        foreach (var component in _attachedComponents)
        {
            if (seen.Add(component))
                list.Add(component);
        }

        return list;
    }

    public IReadOnlyCollection<Component>
        GetAllComponentsToAttach()
    {
        var list =
            new List<Component>(
                _componentsToAttach.Count);

        list.AddRange(
            _componentsToAttach);

        return list;
    }

    public void ClearLists()
    {
        _scene = null;

        _attachedComponents.Clear();
        _updatableComponents.Clear();
        _physicsUpdatableComponents.Clear();
        _componentsToAttach.Clear();
        _componentsToDetach.Clear();
    }

    public void UpdateComponents()
    {
        foreach (var component in _updatableComponents)
        {
            if (component.Enabled)
                component.Update();
        }
    }

    internal void EditorUpdateComponents()
    {
        foreach (var component in _attachedComponents)
        {
            if (component.Enabled)
                component.EditorUpdate();
        }
    }

    public void PhysicsUpdateComponents()
    {
        /*
         * Only components whose type actually overrides OnPhysicsUpdate
         * participate in the fixed physics loop.
         */
        foreach (var component in _physicsUpdatableComponents)
        {
            if (component.Enabled)
                component.PhysicsUpdate();
        }
    }

    public void UpdateLists()
    {
        // Handle creation.
        foreach (var component in _componentsToAttach)
        {
            if (!_attachedComponents.Add(component))
                continue;

            if (component is DrawableComponent drawable &&
                _scene != null)
            {
                _scene.Drawables.Add(drawable);
            }

            if (_scene?.ExecutionMode ==
                SceneExecutionMode.Runtime)
            {
                component.AddToEntity();
            }

            var componentType =
                component.GetType();

            if (OverridesOnUpdate(componentType))
                _updatableComponents.Add(component);

            if (OverridesOnPhysicsUpdate(componentType))
                _physicsUpdatableComponents.Add(component);
        }

        _componentsToAttach.Clear();

        // Handle deletion.
        foreach (var component in _componentsToDetach)
        {
            if (!_attachedComponents.Remove(component))
                continue;

            _updatableComponents.Remove(component);
            _physicsUpdatableComponents.Remove(component);

            if (component is DrawableComponent drawable &&
                _scene != null)
            {
                _scene.Drawables.Remove(drawable);
            }

            component.RemoveFromEntity();
            component.Destroy();

            component.Entity = null;

            component.Dispose();
        }

        _componentsToDetach.Clear();
    }

    private static bool OverridesOnUpdate(
        Type componentType)
    {
        return HasOnUpdateOverrideByType.GetOrAdd(
            componentType,
            static type =>
            {
                var method =
                    type.GetMethod(
                        nameof(Component.OnUpdate),
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        Type.EmptyTypes,
                        null);

                return
                    method is not null &&
                    method.DeclaringType !=
                    typeof(Component) &&
                    method.GetBaseDefinition()
                        .DeclaringType ==
                    typeof(Component);
            });
    }

    private static bool OverridesOnPhysicsUpdate(
        Type componentType)
    {
        return HasOnPhysicsUpdateOverrideByType.GetOrAdd(
            componentType,
            static type =>
            {
                var method =
                    type.GetMethod(
                        nameof(Component.OnPhysicsUpdate),
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        Type.EmptyTypes,
                        null);

                return
                    method is not null &&
                    method.DeclaringType !=
                    typeof(Component) &&
                    method.GetBaseDefinition()
                        .DeclaringType ==
                    typeof(Component);
            });
    }

    internal static void ReleaseAssembly(
        Assembly assembly)
    {
        foreach (var type in
                 HasOnUpdateOverrideByType.Keys
                     .Where(type =>
                         type.Assembly == assembly)
                     .ToArray())
        {
            HasOnUpdateOverrideByType.TryRemove(
                type,
                out _);
        }

        foreach (var type in
                 HasOnPhysicsUpdateOverrideByType.Keys
                     .Where(type =>
                         type.Assembly == assembly)
                     .ToArray())
        {
            HasOnPhysicsUpdateOverrideByType.TryRemove(
                type,
                out _);
        }
    }
}