using System.Collections.Generic;
using System.Linq;
using Type = System.Type;

namespace Dreambit.ECS;

public class ComponentList(Scene scene)
{
    private readonly Logger<ComponentList> _logger = new();

    private readonly HashSet<Component> _attachedComponents = [];
    private readonly HashSet<Component> _componentsToAttach = [];
    private readonly HashSet<Component> _componentsToDetach = [];
    private Scene _scene = scene;

    public void AttachComponent<T>(T component) where T : Component
    {
        if (_componentsToAttach.Contains(component))
            return;

        if (_attachedComponents.Contains(component) || _componentsToDetach.Contains(component))
            return;

        _componentsToAttach.Add(component);
    }


    public void DetachComponent<T>(T component) where T : Component
    {
        if (component == null)
        {
            _logger.Warn("Could not destroy component, component is null");
            return;
        }

        if (_componentsToDetach.Contains(component))
        {
            _logger.Trace("ComponentList: {0} is already being removed", component.GetType().Name);
            return;
        }

        if (_componentsToAttach.Contains(component))
        {
            _componentsToAttach.Remove(component);

            return;
        }

        if (!_componentsToDetach.Contains(component))
            _componentsToDetach.Add(component);
    }

    internal void DestroyAllComponentsNow()
    {
        foreach (var component in _componentsToAttach
                     .Where(c => !_attachedComponents.Contains(c)))
        {
            component.Entity = null;
            component.OnRemovedFromEntity();
            component.Destroy();
            component.Dispose();
        }

        _componentsToAttach.Clear();

        foreach (var component in _attachedComponents)
        {
            if (component is DrawableComponent drawableComponent)
                _scene.Drawables.Remove(drawableComponent);

            component.Entity = null;
            component.OnRemovedFromEntity();
            component.Destroy();
            component.Dispose();
        }

        _attachedComponents.Clear();
        _componentsToDetach.Clear();
    }

    public T GetComponent<T>() where T : Component
    {
        var result = _attachedComponents.FirstOrDefault(c => c is T);
        if (result != null)
            return result as T;

        result = _componentsToAttach.FirstOrDefault(c => c is T);
        if (result != null)
            return result as T;

        return null;
    }

    public bool ComponentOfTypeExists(Type type)
    {
        var result = _attachedComponents.FirstOrDefault(c => type.IsAssignableFrom(c.GetType()));
        if (result != null)
            return true;

        result = _componentsToAttach.FirstOrDefault(c => type.IsAssignableFrom(c.GetType()));
        return result != null;
    }

    public Component GetComponent(Type type)
    {
        var result = _attachedComponents.FirstOrDefault(c => c.GetType() == type);
        if (result != null)
            return result;

        result = _componentsToAttach.FirstOrDefault(c => c.GetType() == type);
        return result != null ? result : null;
    }

    public IReadOnlyCollection<Component> GetAllAttachedComponents()
    {
        return _attachedComponents;
    }

    public IReadOnlyCollection<Component> GetAllActiveComponents()
    {
        return _attachedComponents.Where(x => x.Enabled).ToHashSet();
    }

    public IReadOnlyCollection<Component> GetAllComponents()
    {
        var result = new List<Component>();

        result.AddRange(_componentsToAttach);
        result.AddRange(_attachedComponents);

        return result.ToHashSet();
    }

    public void ClearLists()
    {
        _scene = null;
        _attachedComponents.Clear();
        _componentsToAttach.Clear();
        _componentsToDetach.Clear();
    }

    public void UpdateComponents()
    {
        foreach (var component in _attachedComponents
                     .Where(x => x.Enabled))
            component.OnUpdate();
    }

    public void UpdateLists()
    {
        //Handle Creation
        foreach (var componentToAdd in _componentsToAttach)
        {
            _attachedComponents.Add(componentToAdd);

            //add to drawables
            if (componentToAdd is DrawableComponent drawableComponent)
                _scene.Drawables.Add(drawableComponent);

            componentToAdd.OnAddedToEntity();
        }

        _componentsToAttach.Clear();

        //Handle Deletion
        foreach (var componentToRemove in _componentsToDetach)
            if (_attachedComponents.Remove(componentToRemove))
            {
                //remove from drawables
                if (componentToRemove is DrawableComponent drawableComponent)
                    _scene.Drawables.Remove(drawableComponent);

                componentToRemove.Entity = null;
                componentToRemove.OnRemovedFromEntity();
                componentToRemove.Destroy();
                componentToRemove.Dispose();
            }

        _componentsToDetach.Clear();
    }
}