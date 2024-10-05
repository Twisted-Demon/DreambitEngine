using System;

namespace PixelariaEngine.ECS;

public abstract class Component : IDisposable
{
    private bool _enabled;
    private bool _isDestroyed;
    private bool _isDisposed;

    public Transform Transform => Entity.Transform;

    public Entity Entity { get; internal set; }
    public Scene Scene => Entity?.Scene;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;

            _enabled = value;

            if (value)
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

    ~Component()
    {
        Dispose(false);
    }

    internal Component SetUp(Entity entity, bool enabled)
    {
        Entity = entity;
        _enabled = enabled;

        ProcessAttributes();

        return this;
    }

    private void ProcessAttributes()
    {
        var attributes = Attribute.GetCustomAttributes(GetType());
        foreach (var attribute in attributes)
        {
            if (attribute is not RequireAttribute requireAttribute) continue;

            var hasRequired = Entity.HasComponentOfType(requireAttribute.RequiredType);
            if (hasRequired) continue;

            Entity.AttachComponent(requireAttribute.RequiredType);
        }
    }

    /// <summary>
    ///     Gets called immediately when the component is created. Imagine this is an Initialization function
    /// </summary>
    public virtual void OnCreated()
    {
    }

    /// <summary>
    ///     Called when the component is destroyed. Called after it has been de-attached from the Entity
    /// </summary>
    public virtual void OnDestroyed()
    {
    }

    /// <summary>
    ///     Called after created, and when component has been attached to the entity.
    /// </summary>
    public virtual void OnAddedToEntity()
    {
    }

    /// <summary>
    ///     Called when has been detached from the entity, but not yet destroyed.
    /// </summary>
    public virtual void OnRemovedFromEntity()
    {
    }

    /// <summary>
    ///     Called when the entity is enabled. This is not called when the component is added to the entity
    ///     for the first time, regardless of the enabled value. This is only called when the enabled value
    ///     has been altered after the component has been added to the entity
    /// </summary>
    public virtual void OnEnabled()
    {
    }

    /// <summary>
    ///     Called when the entity is disabled. This is not called when the component is added to the entity
    ///     for the first time, regardless of the enabled value. This is only called when the enabled value
    ///     has been altered after the component has been added to the entity
    /// </summary>
    public virtual void OnDisabled()
    {
    }

    /// <summary>
    ///     called every update loop of the game
    /// </summary>
    public virtual void OnUpdate()
    {
    }

    internal void Destroy()
    {
        _isDestroyed = true;
        OnDestroyed();
    }

    public static bool operator ==(Component a, Component b)
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

    public static bool operator !=(Component a, Component b)
    {
        return !(a == b);
    }

    public override bool Equals(object obj)
    {
        if (obj is Component otherEntity) return this == otherEntity;

        return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    /// <summary>
    ///     Checks if this entity is null (used as component deletion is usually handled at the
    ///     beginning of every frame.
    /// </summary>
    /// <param name="component"></param>
    /// <returns></returns>
    public static bool IsNull(Component component)
    {
        return component == null || component._isDestroyed;
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

public abstract class Component<T> : Component where T : Component
{
    protected Logger<T> Logger = new();
}