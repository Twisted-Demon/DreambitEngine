using System;
using System.Collections.Generic;
using System.Reflection;

namespace Dreambit.ECS;

public abstract class Component<T> : Component where T : Component
{
    protected new static readonly ILogger Logger = new Logger<T>();
}

public abstract class Component : IDisposable
{
    protected static readonly ILogger Logger = new Logger<Component>();
    private bool _enabled = true;
    private bool _guarded = true;
    private bool _isDisposed;
    internal bool IsDestroyed;
    internal IReadOnlyList<Type> RequiredComponentTypes = [];

    protected ICoroutineService CoroutineService => Core.Instance.CurrentScene.CoroutineService;

    public Transform Transform => Entity?.Transform;

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
                Enable();
            else
                Disable();
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

    internal virtual Component SetUpAndCreateChildren(Entity entity, bool enabled = true)
    {
        Entity = entity;
        _enabled = enabled;

        RequiredComponentTypes = GetRequiredComponents();

        foreach (var cType in RequiredComponentTypes) Entity.AttachComponent(cType);
        MapRequiredFieldComponents();
        return this;
    }

    internal static Component BpFromType(Type type, Entity entity, bool enabled = true)
    {
        if (!type.IsSubclassOf(typeof(Component)))
        {
            Logger.Warn("{0} is not a valid component type on deserialization", type.FullName);
            return null;
        }

        // check if already created, if not create a new one
        var component =
            entity.GetComponent(type) ??
            (Component)Activator.CreateInstance(type);

        if (component is null)
            return null;

        component.Entity = entity;
        component._enabled = enabled;
        component.RequiredComponentTypes = component.GetRequiredComponents();


        return component;
    }

    private IReadOnlyList<Type> GetRequiredComponents()
    {
        var list = new List<Type>();

        var attributes = Attribute.GetCustomAttributes(GetType());
        foreach (var attribute in attributes)
        {
            if (attribute is not RequireAttribute requireAttribute) continue;

            foreach (var requiredType in requireAttribute.RequiredTypes)
            {
                var hasRequired = Entity.HasComponentOfType(requiredType);
                if (hasRequired) continue;

                list.Add(requiredType);
            }
        }

        return list;
    }

    internal void MapRequiredFieldComponents()
    {
        const BindingFlags flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        var type = GetType();

        var fields = type.GetFields(flags);
        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttribute<FromRequiredAttribute>();
            if (attribute == null) continue;

            var requiredType = field.FieldType;
            var requiredComponent = Entity.GetComponent(requiredType);

            if (requiredComponent is null)
                Logger.Warn("{0} unable to reference component. Ensure use of Require component attribute",
                    requiredType.FullName);

            field.SetValue(this, requiredComponent);
        }

        var props = type.GetProperties(flags);
        foreach (var prop in props)
        {
            var attribute = prop.GetCustomAttribute<FromRequiredAttribute>();
            if (attribute == null) continue;

            var requiredType = prop.PropertyType;
            var requiredComponent = Entity.GetComponent(requiredType);

            if (requiredComponent is null)
                Logger.Warn("{0} unable to reference component. Ensure use of Require component attribute",
                    requiredType.FullName);

            prop.SetValue(this, requiredComponent);
        }
    }

    /// <summary>
    ///     Gets called immediately before the component is de-serialized
    /// </summary>
    public virtual void OnBeforeDeserialize()
    {
    }

    /// <summary>
    ///     Gets called immediately after the component is de-serialized
    /// </summary>
    public virtual void OnAfterDeserialize()
    {
    }

    /// <summary>
    ///     Gets called immediately when the component is instantiated and serialized.
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

    /// <summary>
    ///     Called every physics update during the loop of the game
    /// </summary>
    public virtual void OnPhysicsUpdate()
    {
    }

    /// <summary>
    ///     Called if debug mode is activated. Used to render debug data.
    /// </summary>
    public virtual void OnDebugDraw()
    {
    }

    internal void BeforeDeserialize()
    {
        if (!_guarded) return;
        _guarded = Guard.SafeCall(OnBeforeDeserialize, "OnBeforeDeserialize");
    }

    internal void AfterDeserialize()
    {
        if (!_guarded) return;
        _guarded = Guard.SafeCall(OnAfterDeserialize, "OnAfterDeserialize");
    }

    internal void Create()
    {
        if (!_guarded) return;
        _guarded = Guard.SafeCall(OnCreated, "OnCreated");
    }

    internal void AddToEntity()
    {
        if (!_guarded) return;
        _guarded = Guard.SafeCall(OnAddedToEntity, "OnAddedToEntity");
    }

    internal void Update()
    {
        if (!_guarded) return;
        _guarded = Guard.SafeCall(OnUpdate, "OnUpdate");
    }

    internal void PhysicsUpdate()
    {
        if (!_guarded) return;
        _guarded = Guard.SafeCall(OnPhysicsUpdate, "OnPhysicsUpdate");
    }

    internal void RemoveFromEntity()
    {
        if (!_guarded) return;
        _guarded = Guard.SafeCall(OnRemovedFromEntity, "OnRemovedFromEntity");
    }

    internal void Enable()
    {
        if (!_guarded) return;
        _guarded = Guard.SafeCall(OnEnabled, "OnEnable");
    }

    internal void Disable()
    {
        if (!_guarded) return;
        _guarded = Guard.SafeCall(OnDisabled, "OnDisable");
    }

    internal void Destroy()
    {
        IsDestroyed = true;
        _guarded = Guard.SafeCall(OnDestroyed, "OnDestroyed");
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

        if (a.IsDestroyed && ReferenceEquals(b, null)) return true;

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
        return component == null || component.IsDestroyed;
    }

    protected virtual void OnDisposing()
    {
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        if (disposing) OnDisposing();

        IsDestroyed = true;
        _isDisposed = true;
        Entity = null;
    }
}

public class SingletonComponent<T> : Component where T : SingletonComponent<T>
{
    public static T Instance { get; private set; }

    internal override Component SetUpAndCreateChildren(Entity entity, bool enabled)
    {
        Instance = this as T;

        return base.SetUpAndCreateChildren(entity, enabled);
    }
}