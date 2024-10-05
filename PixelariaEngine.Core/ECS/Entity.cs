using System;

namespace PixelariaEngine.ECS;

public class Entity : IDisposable
{
    private readonly ComponentList _componentList;
    public readonly uint Id;
    public readonly Transform Transform;

    private bool _enabled;
    private bool _isDestroyed;
    private bool _isDisposed;
    public string Name;

    internal Scene Scene;
    public string Tag;

    internal Entity(uint id, string name, string tag, bool enabled, Scene scene)
    {
        Id = id;
        Name = name;
        Tag = tag;
        _enabled = enabled;
        Scene = scene;

        if (Name == "entity")
            Name += $": {id}";

        _componentList = new ComponentList(scene);
        Transform = new Transform();
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

    public static Entity Create(string name = "entity", string tag = "default", bool enabled = true)
    {
        return Core.Instance.CurrentScene.CreateEntity(name, tag, enabled);
    }

    public static void Destroy(Entity entity)
    {
        Core.Instance.CurrentScene.DestroyEntity(entity);
    }

    internal void Update()
    {
        if (_isDestroyed) return;

        _componentList.UpdateLists();
        _componentList.UpdateComponents();
    }

    /// <summary>
    ///     Attaches a component to the entity, if it already exists
    ///     it will return the existing component
    /// </summary>
    /// <typeparam name="T">Component Type</typeparam>
    /// <returns></returns>
    public T AttachComponent<T>() where T : Component
    {
        var component = _componentList.GetComponent<T>();
        if (component != null) return component;

        component = (T)Activator.CreateInstance<T>().SetUp(this, true);

        if (component == null)
            return null;

        component.OnCreated();
        _componentList.AttachComponent(component);

        return component;
    }

    public Component AttachComponent(Type type)
    {
        var component = _componentList.GetComponent(type);
        if (component != null) return component;

        component = Activator.CreateInstance(type) as Component;
        if (component == null) return null;
        component.SetUp(this, true);

        component.OnCreated();
        _componentList.AttachComponent(component);

        return component;
    }

    /// <summary>
    ///     Detaches a component from the entity and cleans it up
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void DetachComponent<T>() where T : Component
    {
        var componentToRemove = _componentList.GetComponent<T>();

        if (componentToRemove == null)
            return;

        _componentList.DetachComponent(componentToRemove);
    }

    /// <summary>
    ///     Detaches a component from the entity and cleans it up
    ///     only if it exists in the entities internal component list.
    /// </summary>
    /// <param name="component"></param>
    /// <typeparam name="T"></typeparam>
    public void DetachComponent<T>(T component) where T : Component
    {
        _componentList.DetachComponent(component);
    }

    /// <summary>
    ///     Retrieves a component by Type
    /// </summary>
    /// <typeparam name="T">Component Type</typeparam>
    /// <returns></returns>
    public T GetComponent<T>() where T : Component
    {
        return _componentList.GetComponent<T>();
    }

    internal bool HasComponentOfType(Type componentType)
    {
        return _componentList.ComponentOfTypeExists(componentType);
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

    internal void Destroy()
    {
        _isDestroyed = true;
        Scene = null;
        _componentList.DestroyAllComponentsNow();
        _componentList.ClearLists();
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