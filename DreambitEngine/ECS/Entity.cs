using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public class Entity : IDisposable
{
    private readonly List<Entity> _children = [];

    private readonly ILogger _logger = new Logger<Entity>();

    public readonly Guid Id;

    private bool _alwaysUpdate;
    private bool _enabled;
    private bool _isDestroyed;
    private bool _isDisposed;
    private Entity _parent;
    public string Name;

    internal Entity(Guid id, string name, HashSet<string> tags, bool enabled, Scene scene)
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

        ComponentRepository = new ComponentRepository(scene);
        Transform = new Transform(this);
    }

    private Entity()
    {
    }

    private ComponentRepository ComponentRepository { get; }
    public Transform Transform { get; }
    public HashSet<string> Tags { get; } = [];
    internal Scene Scene { get; private set; }

    public Entity Parent
    {
        get => _parent;
        set
        {
            if (_parent == value) return;
            SetParent(value);
        }
    }

    public bool AlwaysUpdate
    {
        get => _alwaysUpdate;
        set
        {
            if (_alwaysUpdate == value) return;
            _alwaysUpdate = value;

            foreach (var child in _children)
                child.AlwaysUpdate = value;

            Scene.SetEntityAlwaysUpdate(this, value);
        }
    }

    public bool Enabled
    {
        get
        {
            if (Parent == null)
                return _enabled;

            return Parent.Enabled && _enabled;
        }
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

    public static Entity Create(
        string name = "entity",
        HashSet<string> tags = null,
        bool enabled = true,
        Vector3? createAt = null,
        Vector3? rotation = null,
        Vector3? scale = null,
        Guid? guidOverride = null)
    {
        var entity =
            Core.Instance.CurrentScene.CreateEntity(name, tags, enabled, createAt, rotation, scale, guidOverride);

        entity.Transform.LastWorldPosition = entity.Transform.WorldPosition;
        return entity;
    }

    public static Entity Create(
        EntityBlueprint blueprint,
        bool enabled = true,
        Vector3? createAt = null,
        Vector3? rotation = null,
        Vector3? scale = null)
    {
        var entity = Core.Instance.CurrentScene.CreateEntity(blueprint, enabled, createAt, rotation, scale);

        entity.Transform.LastWorldPosition = entity.Transform.WorldPosition;

        return entity;
    }

    public static Entity CreateChildOf(
        Entity parent,
        string name = "entity",
        HashSet<string> tags = null,
        bool enabled = true)
    {
        var entity = Core.Instance.CurrentScene.CreateEntity(name, tags, enabled);
        entity.Parent = parent;

        entity.Transform.LastWorldPosition = entity.Transform.WorldPosition;
        return entity;
    }

    public static Entity FindByName(string name)
    {
        var entity = Core.Instance.CurrentScene.FindEntity(name);

        return entity;
    }

    public static Entity FindById(Guid iid)
    {
        var entity = Core.Instance.CurrentScene.FindEntity(iid);

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
        if (entity == null || entity._isDestroyed) return;

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

        ComponentRepository.UpdateLists();
        ComponentRepository.UpdateComponents();
    }

    internal void PhysicsUpdate()
    {
        if (_isDestroyed) return;
        ComponentRepository.PhysicsUpdateComponents();
    }

    /// <summary>
    ///     Attaches a component to the entity, if it already exists
    ///     it will return the existing component
    /// </summary>
    /// <typeparam name="T">Component Type</typeparam>
    /// <returns></returns>
    public T AttachComponent<T>() where T : Component
    {
        var component = ComponentRepository.GetComponent<T>();
        if (component != null) return component;

        component = (T)Activator.CreateInstance<T>().SetUpAndCreateChildren(this);

        if (component == null)
            return null;

        component.Create();
        ComponentRepository.AttachComponent(component);

        return component;
    }

    /// <summary>
    ///     Attaches a component to the entity, if it already exists
    ///     it will return the existing component
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public Component AttachComponent(Type type)
    {
        var component = ComponentRepository.GetComponent(type);
        if (component != null) return component;

        if (type is null || !type.IsSubclassOf(typeof(Component)))
            return null;

        component = (Component)Activator.CreateInstance(type);
        if (component == null) return null;
        component.SetUpAndCreateChildren(this);

        component.Create();
        ComponentRepository.AttachComponent(component);

        return component;
    }

    internal void BuildComponentsFromBlueprint(EntityBlueprint entityBlueprint)
    {
        // all components we will end up creating (one per Type)
        var constructed = new Dictionary<Type, Component>();

        //Seed all component types explicitly declared in the blueprint
        var pending = new Stack<Type>();
        foreach (var cbp in entityBlueprint.Components)
        {
            var type = BlueprintResolver.ResolveComponentType(cbp.Type);
            if (type == null)
            {
                _logger.Warn("{0} is not a valid component type", cbp.Type);
                continue;
            }

            pending.Push(type);
        }

        // Expand transitive requirements
        while (pending.Count > 0)
        {
            var type = pending.Pop();
            if (constructed.ContainsKey(type))
                continue; // already constructed skip

            var component = Component.BpFromType(type, this);
            if (component == null)
            {
                _logger.Warn("Could not construct component of type {0} for entity {1}", type.FullName, Name);
                continue;
            }

            constructed[type] = component;

            // Enqueue all direct requirements to be created
            foreach (var requiredType in component.RequiredComponentTypes)
            {
                if (requiredType is null) continue;
                if (!constructed.ContainsKey(requiredType))
                    pending.Push(requiredType);
            }
        }

        foreach (var component in constructed.Values)
            ComponentRepository.AttachComponent(component);
    }

    internal void DeserializeComponentsFromBlueprints(EntityBlueprint ebp)
    {
        var components = new Dictionary<Type, Component>();

        foreach (var component in ComponentRepository.GetAllComponentsToAttach())
        {
            components[component.GetType()] = component;
            component.MapRequiredFieldComponents();
        }

        foreach (var componentBlueprint in ebp.Components)
        {
            var type = BlueprintResolver.ResolveComponentType(componentBlueprint.Type);
            if (type == null) continue;
            if (!components.TryGetValue(type, out var component) || component == null) continue;

            component.BeforeDeserialize();
            try
            {
                BlueprintResolver.ResolveComponent(componentBlueprint, component);
            }
            catch (Exception e)
            {
                _logger.Error($"{e.Message}");
            }

            component.AfterDeserialize();
        }
    }

    internal void CallComponentOnCreateAfterDeserialized()
    {
        var components = ComponentRepository.GetAllComponentsToAttach();
        foreach (var component in components)
            component.Create();
    }

    /// <summary>
    ///     Detaches a component from the entity and cleans it up
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void DetachComponent<T>() where T : Component
    {
        var componentToRemove = ComponentRepository.GetComponent<T>();

        if (componentToRemove == null)
            return;

        ComponentRepository.DetachComponent(componentToRemove);
    }

    /// <summary>
    ///     Detaches a component from the entity and cleans it up
    ///     only if it exists in the entities internal component list.
    /// </summary>
    /// <param name="component"></param>
    /// <typeparam name="T"></typeparam>
    public void DetachComponent<T>(T component) where T : Component
    {
        ComponentRepository.DetachComponent(component);
    }

    /// <summary>
    ///     Retrieves a component by Type
    /// </summary>
    /// <typeparam name="T">Component Type</typeparam>
    /// <returns></returns>
    public T GetComponent<T>() where T : Component
    {
        return ComponentRepository.GetComponent<T>();
    }

    public Component GetComponent(Type type)
    {
        if (type is null || !type.IsSubclassOf(typeof(Component)))
            return null;

        return ComponentRepository.GetComponent(type);
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
    ///     gets a list of all components that are attached, regardless if they are active
    /// </summary>
    /// <returns></returns>
    public IReadOnlyCollection<Component> GetAllAttachedComponents()
    {
        return ComponentRepository.GetAllAttachedComponents();
    }

    /// <summary>
    ///     gets a list of all components only if they are active
    /// </summary>
    /// <returns></returns>
    public IReadOnlyCollection<Component> GetAllActiveComponents()
    {
        return ComponentRepository.GetAllActiveComponents();
    }

    public IReadOnlyCollection<Component> GetAllComponents()
    {
        return ComponentRepository.GetAllComponents();
    }

    internal bool HasComponentOfType(Type componentType)
    {
        return ComponentRepository.ComponentOfTypeExists(componentType);
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
        ComponentRepository.DestroyAllComponentsNow();
        ComponentRepository.ClearLists();
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