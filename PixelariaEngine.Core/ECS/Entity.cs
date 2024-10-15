using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.Xna.Framework;

namespace PixelariaEngine.ECS;

public class Entity : IDisposable
{
    private Entity _parent;
    private ComponentList ComponentList { get; set; }
    private bool _alwaysUpadte = false;
    public Transform Transform { get; private set; }
    public HashSet<string> Tags { get; private set; } = [];
    internal Scene Scene { get; private set; }
    
    public readonly uint Id;
    private bool _enabled;
    private bool _isDestroyed;
    private bool _isDisposed;
    public string Name;
    
    public Entity Parent
    {
        get => _parent;
        set
        {
            if(_parent == value) return;
            SetParent(value);
        }
    }

    public bool AlwaysUpdate
    {
        get => _alwaysUpadte;
        set
        {
            if(_alwaysUpadte == value) return;
            _alwaysUpadte = value;

            foreach (var child in _children)
                child.AlwaysUpdate = value;

            Scene.SetEntityAlwaysUpdate(this, value);
        }
    }

    private readonly List<Entity> _children = [];

    internal Entity(uint id, string name, HashSet<string> tags, bool enabled, Scene scene)
    {
        Id = id;
        Name = name;
        _enabled = enabled;
        Scene = scene;

        if (tags == null)
            Tags = ["default"];
        else
            foreach (var tag in tags)
                Tags.Add(tag);

        if (Name == "entity")
            Name += $": {id}";

        ComponentList = new ComponentList(scene);
        Transform = new Transform(this);
    }

    private Entity()
    {
    }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;
            _enabled = value;

            if (_enabled)
                OnEnabled();
            else
                OnDisabled();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~Entity()
    {
        Dispose(false);
    }

    public static Entity Create(string name = "entity", HashSet<string> tags = null
        , bool enabled = true, Vector3? createAt = null)
    {
        var entity = Core.Instance.CurrentScene.CreateEntity(name, tags, enabled, createAt);

        entity.Transform.LastWorldPosition = entity.Transform.WorldPosition;
        return entity;
    }

    public static Entity CreateChildOf(Entity parent, string name = "entity", HashSet<string> tags = null,
        bool enabled = true)
    {
        var entity = Core.Instance.CurrentScene.CreateEntity(name, tags, enabled);
        entity.Parent = parent;

        entity.Transform.LastWorldPosition = entity.Transform.WorldPosition;
        return entity;
    }

    public static Entity FindByName(string name)
    {
        var entity = Core.Instance.CurrentScene.GetEntity(name);

        return entity;
    }

    public List<Entity> GetChildren()
    {
        var children = new List<Entity>();
        children.AddRange(_children);

        return children;
    }

    public static void Destroy(Entity entity)
    {
        Core.Instance.CurrentScene.DestroyEntity(entity);

        if (entity._children.Count <= 0) return;
        
        foreach (var child in entity._children)
            Destroy(child);
    }

    public static bool CompareTag(Component component, string tag)
    {
        return component.Entity.Tags.Contains(tag);
    }

    internal void UpdateTransform()
    {
        if (Transform.LastWorldPosition != Transform.WorldPosition)
        {
        }
    }

    internal void Update()
    {
        if (_isDestroyed) return;

        ComponentList.UpdateLists();
        ComponentList.UpdateComponents();
    }

    /// <summary>
    ///     Attaches a component to the entity, if it already exists
    ///     it will return the existing component
    /// </summary>
    /// <typeparam name="T">Component Type</typeparam>
    /// <returns></returns>
    public T AttachComponent<T>() where T : Component
    {
        var component = ComponentList.GetComponent<T>();
        if (component != null) return component;

        component = (T)Activator.CreateInstance<T>().SetUp(this, true);

        if (component == null)
            return null;

        component.OnCreated();
        ComponentList.AttachComponent(component);

        return component;
    }

    public Component AttachComponent(Type type)
    {
        var component = ComponentList.GetComponent(type);
        if (component != null) return component;

        component = Activator.CreateInstance(type) as Component;
        if (component == null) return null;
        component.SetUp(this, true);

        component.OnCreated();
        ComponentList.AttachComponent(component);

        return component;
    }

    /// <summary>
    ///     Detaches a component from the entity and cleans it up
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void DetachComponent<T>() where T : Component
    {
        var componentToRemove = ComponentList.GetComponent<T>();

        if (componentToRemove == null)
            return;

        ComponentList.DetachComponent(componentToRemove);
    }

    /// <summary>
    ///     Detaches a component from the entity and cleans it up
    ///     only if it exists in the entities internal component list.
    /// </summary>
    /// <param name="component"></param>
    /// <typeparam name="T"></typeparam>
    public void DetachComponent<T>(T component) where T : Component
    {
        ComponentList.DetachComponent(component);
    }

    /// <summary>
    ///     Retrieves a component by Type
    /// </summary>
    /// <typeparam name="T">Component Type</typeparam>
    /// <returns></returns>
    public T GetComponent<T>() where T : Component
    {
        return ComponentList.GetComponent<T>();
    }

    public T GetComponentInChildren<T>() where T : Component
    {
        foreach (var child in _children)
        {
            //check if we have the component
            var component = child.GetComponent<T>();
            
            //if we do have it, return
            if (component != null) return component;
            
            //if we don't check in children
            component = child.GetComponentInChildren<T>();
        }
        
        //only get here if no children have component
        return null;
    }

    /// <summary>
    /// gets a list of all components that are attached, regardless if they are active
    /// </summary>
    /// <returns></returns>
    public IReadOnlyCollection<Component> GetAllAttachedComponents()
    {
        return ComponentList.GetAllAttachedComponents();
    }
    
    /// <summary>
    /// gets a list of all components only if they are active
    /// </summary>
    /// <returns></returns>
    public IReadOnlyCollection<Component> GetAllActiveComponents()
    {
        return ComponentList.GetAllActiveComponents();
    }

    public IReadOnlyCollection<Component> GetAllComponents()
    {
        return ComponentList.GetAllComponents();
    }

    internal bool HasComponentOfType(Type componentType)
    {
        return ComponentList.ComponentOfTypeExists(componentType);
    }

    internal void OnAddedToScene()
    {
        //Todo: Implement on call back for components
    }

    internal void OnRemovedFromScene()
    {
        //Todo: Implement on call back for components
    }

    internal void OnEnabled()
    {
        //Todo: Implement on call back for components
    }

    internal void OnDisabled()
    {
        //Todo: Implement on call back for components
    }

    private void SetParent(Entity parentEntity)
    {
        if (_parent != null)
            _parent._children.Remove(this);
        
        _parent = parentEntity;
        parentEntity._children.Add(this);
    }

    internal void Destroy()
    {
        _isDestroyed = true;
        Scene = null;
        ComponentList.DestroyAllComponentsNow();
        ComponentList.ClearLists();
    }

    public static bool operator ==(Entity a, Entity b)
    {
        switch (a)
        {
            // Handle both being null
            case null when ReferenceEquals(b, null):
                return true;
            case null:
                return false;
        }

        if (a._isDestroyed && ReferenceEquals(b, null)) return true;

        return ReferenceEquals(a, b);
    }

    public static bool operator !=(Entity a, Entity b)
    {
        return !(a == b);
    }

    public override bool Equals(object obj)
    {
        if (obj is Entity otherEntity) return this == otherEntity;

        return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public static bool IsNull(Entity entity)
    {
        return entity == null || entity._isDestroyed;
    }

    protected virtual void OnDisposing()
    {
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        if (disposing) OnDisposing();

        _isDestroyed = true;
        _isDisposed = true;
    }
}