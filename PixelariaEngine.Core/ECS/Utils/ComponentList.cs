using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelariaEngine.ECS;

public class ComponentList(Scene scene)
{
    private readonly List<Component> _attachedComponents = [];
    private readonly List<Component> _componentsToAttach = [];
    private readonly List<Component> _componentsToDetach = [];
    private readonly Scene _scene = scene;

    public void AttachComponent<T>(T component) where T : Component
    {
        if (_componentsToAttach.Contains(component))
            return;

        _componentsToAttach.Add(component);
        
        //register it if it is a drawable component
    }

    public void DetachComponent<T>(T component) where T : Component
    {
        if (component == null)
        {
            Console.WriteLine("ComponentList: Could not destroy component, component is null");
            return;
        }

        if (_componentsToDetach.Contains(component))
        {
            Console.WriteLine("ComponentList: {0} is already being removed", component.GetType().Name);
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
            component.OnRemovedFromEntity();
            component.Destroy();
            component.Dispose();
        }

        _componentsToAttach.Clear();

        foreach (var component in _attachedComponents)
        {
            component.OnRemovedFromEntity();
            
            if(component is DrawableComponent drawableComponent)
                _scene.Drawables.Remove(drawableComponent);
            
            component.Destroy();
            component.Dispose();
        }

        _attachedComponents.Clear();
        _componentsToDetach.Clear();
    }

    public T GetComponent<T>() where T : Component
    {
        var result = _attachedComponents.FirstOrDefault(component => component.GetType() == typeof(T));
        if (result != null)
            return result as T;

        result = _componentsToAttach.FirstOrDefault(c => c.GetType() == typeof(T));
        return result as T;
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
        foreach (var componentToAdd in _componentsToAttach
                     .Where(componentToAdd => !_attachedComponents.Contains(componentToAdd)))
        {
            _attachedComponents.Add(componentToAdd);
            
            //add to drawables
            if (componentToAdd is DrawableComponent drawableComponent)
                _scene.Drawables.Add(drawableComponent);
            
            componentToAdd.OnAddedToEntity();
        }

        _componentsToAttach.Clear();

        //Handle Deletion
        foreach (var componentToRemove in _componentsToDetach
                     .Where(x => _attachedComponents.Contains(x)))
        {
            _attachedComponents.Remove(componentToRemove);
            
            //remove from drawables
            if(componentToRemove is DrawableComponent drawableComponent)
                _scene.Drawables.Remove(drawableComponent);
            
            componentToRemove.OnRemovedFromEntity();
            componentToRemove.Destroy();
            componentToRemove.Dispose();
        }

        _componentsToDetach.Clear();
    }
}