using System;

namespace PixelariaEngine.ECS;

public class Entity : IDisposable
{
    private readonly ComponentList _componentList;
    public readonly uint Id;

    private bool _enabled;
    private bool _isDestroyed;
    private bool _isDisposed;
    public string Name;
    public Scene Scene;
    public string Tag;
    public Transform Transform;

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

    internal void Update()
    {
        if (_isDestroyed) return;

        _componentList.UpdateLists();
        _componentList.UpdateComponents();
    }

    public T AttachComponent<T>() where T : Component
    {
        var component = (T)Activator.CreateInstance<T>().SetUp(this, true);

        if (component == null)
            return null;

        component.OnCreated();
        _componentList.AttachComponent(component);

        return component;
    }

    public void DetachComponent<T>() where T : Component
    {
        var componentToRemove = _componentList.GetComponent<T>();

        if (componentToRemove == null)
            return;

        _componentList.DetachComponent(componentToRemove);
    }

    public void DetachComponent<T>(T component) where T : Component
    {
        _componentList.DetachComponent(component);
    }

    public T GetComponent<T>() where T : Component
    {
        return _componentList.GetComponent<T>();
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
        _componentList.DestroyAllComponentsNow();
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