using System;
using System.Collections.Generic;
using System.Linq;
using Dreambit.ECS;
using Dreambit.Scripting;
using Microsoft.Xna.Framework;

namespace Dreambit;

/// <summary>
///     Core scene type. Manages entities, drawables, render pipeline, cameras, and lifecycle.
/// </summary>
public class Scene : IDisposable
{
    #region Constructor

    /// <summary>
    ///     Initializes base repositories, managers, and the render pipeline.
    /// </summary>
    public Scene()
    {
        Entities = new EntityRepository(this);
        Drawables = new DrawableRepository();
        ScriptingManager = new ScriptingManager();
        _coroutineScheduler = new CoroutineScheduler();

        PostProcessSettings = new PostProcessSettings();
        RenderingOptions = new RenderingOptions();

        _renderPipeline = new RenderPipeline(this);
        State = SceneState.Created;
    }

    #endregion

    #region IDisposable

    /// <summary>
    ///     Disposes of the scene and transitions to the Disposed state.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;
        Transition(SceneState.Disposed);
        Cleanup();
        _isDisposed = true;
    }

    #endregion

    #region Cutscene Helpers

    /// <summary>
    ///     Helper to start a cutscene by name using the scene's ScriptingManager.
    /// </summary>
    public static void StartCutscene(string cutsceneName, string fileExtension = ".yaml")
    {
        Core.Instance.CurrentScene.ScriptingManager.StartCutscene(cutsceneName, fileExtension);
    }

    #endregion

    #region Scene Switching (Core integration)

    /// <summary>Schedules a new scene to be swapped in by the Core.</summary>
    public static void SetNextScene(Scene scene)
    {
        Core.Instance.SetNextScene(scene);
    }

    /// <summary>Schedules a new scene by type to be swapped in by the Core.</summary>
    public static void SetNextScene<T>() where T : Scene, new()
    {
        var scene = new T();
        Core.Instance.SetNextScene(scene);
    }

    #endregion

    #region Fields (Internals)

    /// <summary>Logger for this scene.</summary>
    private readonly ILogger _logger = new Logger<Scene>();

    /// <summary>Drawables repository for render-ordered components.</summary>
    internal readonly DrawableRepository Drawables;

    /// <summary>Entities repository for ECS management.</summary>
    internal readonly EntityRepository Entities;

    /// <summary>Render pipeline composed of render passes.</summary>
    private RenderPipeline _renderPipeline;

    /// <summary>Tracks disposal state to avoid double-dispose.</summary>
    private bool _isDisposed;

    private readonly CoroutineScheduler _coroutineScheduler;

    #endregion

    #region Public Members & Properties

    /// <summary>Convenience access to the active scene from the core.</summary>
    public static Scene Instance => Core.Instance.CurrentScene;

    /// <summary>Access to the coroutine system</summary>
    public ICoroutineService CoroutineService => _coroutineScheduler;

    /// <summary>Current scene lifecycle state.</summary>
    public SceneState State { get; internal set; }

    /// <summary>Enables engine-level debug drawing and diagnostics.</summary>
    public bool DebugMode { get; set; }

    /// <summary>Cutscene / script driver for the scene.</summary>
    public readonly ScriptingManager ScriptingManager;

    /// <summary>Clear color used before drawing this scene.</summary>
    public Color BackgroundColor = Color.CornflowerBlue;

    /// <summary>Post-process configuration shared with passes.</summary>
    public readonly PostProcessSettings PostProcessSettings;

    public readonly RenderingOptions RenderingOptions;

    /// <summary>Primary world camera.</summary>
    public Camera2D MainCamera { get; private set; }

    /// <summary>UI camera for screen-space/UI rendering.</summary>
    public Camera2D UICamera { get; private set; }

    /// <summary>Ambient light for the scene/// </summary>
    public AmbientLight2D AmbientLight { get; private set; }

    #endregion

    #region Lifecycle Hooks (for derived scenes to override)

    /// <summary>
    ///     Called after the scene has been created, but before actually running.
    ///     Load assets and set up scene content here.
    /// </summary>
    protected virtual void OnInitialize()
    {
    }

    /// <summary>
    ///     Called after initialization when the scene actually begins running.
    ///     Perform any start-time logic here.
    /// </summary>
    protected virtual void OnBegin()
    {
    }

    /// <summary>
    ///     Called once per frame while running.
    ///     Place per-frame logic here (input, gameplay updates, etc.).
    /// </summary>
    protected virtual void OnUpdate()
    {
    }

    protected virtual void OnPhysicsUpdate()
    {
    }

    /// <summary>
    ///     Called when the scene is ending. Clean up scene-specific content here.
    /// </summary>
    protected virtual void OnEnd()
    {
    }

    #endregion

    #region Internal Lifecycle Management

    /// <summary>
    ///     Creates cameras, configures the render pipeline, and invokes initialization.
    /// </summary>
    internal virtual void InitializeInternals()
    {
        _logger.Debug("Initializing Scene");

        // Create default cameras (world + UI)
        MainCamera = Entity.Create("main-camera").AttachComponent<Camera2D>();
        MainCamera.Entity.AlwaysUpdate = true;

        UICamera = Entity.Create("ui-camera").AttachComponent<Camera2D>();
        UICamera.Entity.AlwaysUpdate = true;

        AmbientLight = Entity.Create("ambient-light").AttachComponent<AmbientLight2D>();

        // Setup default render passes
        SetUpRenderPipeLine();
    }

    /// <summary>
    ///     Sets up the default render pass (can be overriden by user)
    /// </summary>
    protected virtual void SetUpRenderPipeLine()
    {
        _renderPipeline.Initialize();
        _renderPipeline.AddRenderPass<Basic2dLightingRenderPass>();
        _renderPipeline.AddRenderPass<DebugRenderPass>();
        _renderPipeline.AddRenderPass<PostProcessRenderPass>();
        _renderPipeline.AddRenderPass<UIRenderPass>();
    }

    /// <summary>
    ///     Updates internal services/managers each frame (scripting, ECS tick).
    /// </summary>
    private void UpdateInternals()
    {
        _coroutineScheduler.Update();
        ScriptingManager.Update();
        Entities.OnTick();
    }

    private void EndOfFrame()
    {
        _coroutineScheduler.EndOfFrame();
    }

    /// <summary>
    ///     Clears repositories and disposes the render pipeline.
    /// </summary>
    private void Cleanup()
    {
        Entities.ClearLists();
        Drawables.ClearLists();

        _renderPipeline?.Dispose();
        _renderPipeline = null;
    }

    #endregion

    #region External Lifecycle Control (Engine calls)

    /// <summary>
    ///     Requests scene termination and transitions to ending state.
    /// </summary>
    internal void Terminate()
    {
        if (_isDisposed || State == SceneState.Disposed) return;
        if (State == SceneState.Ending) return;

        Transition(SceneState.Ending);
        Guard.SafeCall(OnEnd, "OnEnd");
        Dispose();
    }

    /// <summary>
    ///     Per-frame driver. Advances scene state machine and calls hooks.
    /// </summary>
    public virtual void Tick()
    {
        if (_isDisposed) return;

        switch (State)
        {
            case SceneState.Created:
                Transition(SceneState.Initializing);
                InitializeInternals();
                Guard.SafeCall(OnInitialize, "OnInitialize");
                Transition(SceneState.Starting);
                Guard.SafeCall(OnBegin, "OnBegin");
                Transition(SceneState.Running);
                break;

            case SceneState.Starting:
            case SceneState.Initializing:
                // Transitional states do not execute per-frame logic.
                break;

            case SceneState.Running:
                UpdateInternals();
                Guard.SafeCall(OnUpdate, "OnUpdate");
                EndOfFrame();
                break;

            case SceneState.Ending:
            case SceneState.Disposed:
                // Scene is shutting down or already disposed.
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    ///     Physics-step driver. Called at a fixed timestep by the engine.
    /// </summary>
    public virtual void PhysicsTick()
    {
        if (State == SceneState.Running)
        {
            Guard.SafeCall(OnPhysicsUpdate, "OnPhysicsUpdate");
            Guard.SafeCall(Entities.OnPhysicsTick, "Entities.OnPhysicsTick");
            Guard.SafeCall(_coroutineScheduler.FixedUpdate,  "Coroutines.FixedUpdate");
        }
    }

    /// <summary>
    ///     Draw driver. Calls the render pipeline when running.
    /// </summary>
    public virtual void OnDraw()
    {
        if (State != SceneState.Running) return;

        Guard.SafeCall(_renderPipeline.OnDraw, "RenderPipeline.OnDraw");
    }

    #endregion

    #region State Machine Helpers

    /// <summary>
    ///     Performs a guarded transition between lifecycle states.
    /// </summary>
    private void Transition(SceneState next)
    {
        if (!IsValidTransition(State, next))
        {
            _logger.Error($"Invalid state transition {State} -> {next}");
            throw new InvalidOperationException($"Invalid state transition {State} -> {next}");
        }

        _logger.Trace($"Scene state: {State} -> {next}");
        State = next;
    }

    /// <summary>
    ///     Validates whether a transition is allowed from one state to another.
    /// </summary>
    private static bool IsValidTransition(SceneState from, SceneState to)
    {
        return (from, to) switch
        {
            (SceneState.Created, SceneState.Initializing) => true,
            (SceneState.Initializing, SceneState.Starting) => true,
            (SceneState.Starting, SceneState.Running) => true,
            (SceneState.Running, SceneState.Ending) => true,
            (SceneState.Ending, SceneState.Disposed) => true,
            _ => false
        };
    }

    #endregion

    #region Entity Management (Facade over EntityRepository)

    /// <summary>
    ///     Creates an entity with optional parameters forwarded to the repository.
    /// </summary>
    public Entity CreateEntity(
        string name = "entity",
        HashSet<string> tags = null,
        bool enabled = true,
        Vector3? createAt = null,
        Vector3? rotation = null,
        Vector3? scale = null,
        Guid? guidOverride = null)
    {
        var entity = Entities.CreateEntity(name, tags, enabled, createAt, rotation, scale, guidOverride);
        return entity;
    }

    /// <summary>
    ///     Creates an entity based on a blueprint, forwarded to the repository
    /// </summary>
    /// <param name="blueprint"></param>
    /// <param name="enabled"></param>
    /// <param name="createAt"></param>
    /// <param name="rotation"></param>
    /// <param name="scale"></param>
    /// <returns></returns>
    public Entity CreateEntity(
        EntityBlueprint blueprint,
        bool? enabled = true,
        Vector3? createAt = null,
        Vector3? rotation = null,
        Vector3? scale = null)
    {
        var pos = blueprint.Position;
        var rot = blueprint.Rotation;
        var scl = blueprint.Scale;
        var en = blueprint.Enabled;

        if (createAt.HasValue)
            pos = createAt.Value;
        if (rotation.HasValue)
            rot = rotation.Value;
        if (scale.HasValue)
            scl = scale.Value;
        if(enabled.HasValue)
            en = enabled.Value;

        var rootEntity = Entities.CreateEntity(blueprint.Name, blueprint.Tags, en, pos, rot, scl);
        blueprint.WorldGuid = rootEntity.Id;
        foreach (var childBp in blueprint.Children)
        {
            CreateChildOfEntity(childBp, rootEntity);
        }

        var entityFamilyTree = blueprint.FlattenedHirearchy().ToArray();
        
        
        foreach (var bp in entityFamilyTree)
        {
            var entity = FindEntity(bp.WorldGuid);
            entity.BuildComponentsFromBlueprint(bp);
        }
        foreach (var bp in entityFamilyTree)
        {
            var entity = FindEntity(bp.WorldGuid);
            entity.DeserializeComponentsFromBlueprints(bp);
        }
        foreach (var bp in entityFamilyTree)
        {
            var entity = FindEntity(bp.WorldGuid);
            entity.CallComponentOnCreateAfterDeserialized();
        }
        
        return rootEntity;
    }

    public Entity CreateChildOfEntity(
        EntityBlueprint blueprint,
        Entity parent)
    {
        var pos = blueprint.Position;
        var rot = blueprint.Rotation;
        var scl = blueprint.Scale;
        var en = blueprint.Enabled;
        
        
        var entity = Entities.CreateEntity(blueprint.Name, blueprint.Tags, blueprint.Enabled, pos, 
            rot, scl);
        blueprint.WorldGuid = entity.Id;
        
        entity.Parent = parent;
        foreach (var childBp in blueprint.Children)
        {
            CreateChildOfEntity(childBp, entity);
        }
        
        return entity;
    }


    /// <summary>Sets AlwaysUpdate on a specific entity.</summary>
    public void SetEntityAlwaysUpdate(Entity entity, bool value)
    {
        Entities.SetEntityAlwaysUpdate(entity, value);
    }

    /// <summary>Destroys a specific entity.</summary>
    public void DestroyEntity(Entity entity)
    {
        Entities.DestroyEntity(entity);
    }

    /// <summary>Finds an entity by GUID.</summary>
    public Entity FindEntity(Guid id)
    {
        return Entities.GetEntity(id);
    }

    /// <summary>Finds an entity by name.</summary>
    public Entity FindEntity(string name)
    {
        return Entities.GetEntity(name);
    }

    /// <summary>Returns all currently active entities.</summary>
    public IReadOnlyList<Entity> GetAllActiveEntities()
    {
        return Entities.GetAllActiveEntities();
    }

    /// <summary>Returns all entities with a given tag.</summary>
    public IReadOnlyList<Entity> GetEntitiesByTag(string tag)
    {
        return Entities.GetEntitiesByTag(tag);
    }

    /// <summary>Returns all active entities with a given tag.</summary>
    public IReadOnlyList<Entity> GetActiveEntitiesByTag(string tag)
    {
        return Entities.GetActiveEntitiesByTag(tag);
    }

    #endregion

    #region LDtk Scene Helpers

    /// <summary>
    ///     Creates and schedules the next LDtk scene by identifier.
    /// </summary>
    public static LDtkScene SetNextLDtkScene(string identifier)
    {
        var scene = new LDtkScene
        {
            LevelIdentifier = identifier,
            LoadByGuid = false
        };

        SetNextScene(scene);
        return scene;
    }

    /// <summary>
    ///     Creates and schedules the next LDtk scene (generic typed) by identifier.
    /// </summary>
    public static T SetNextLDtkScene<T>(string identifier) where T : LDtkScene, new()
    {
        var scene = new T
        {
            LevelIdentifier = identifier,
            LoadByGuid = false
        };

        SetNextScene(scene);
        return scene;
    }

    /// <summary>
    ///     Creates and schedules the next LDtk scene by IID (GUID).
    /// </summary>
    public static LDtkScene SetNextLDtkScene(Guid iid)
    {
        var scene = new LDtkScene
        {
            LevelIid = iid,
            LoadByGuid = true
        };

        SetNextScene(scene);
        return scene;
    }

    /// <summary>
    ///     Creates and schedules the next LDtk scene (generic typed) by IID (GUID).
    /// </summary>
    public static T SetNextLDtkScene<T>(Guid iid) where T : LDtkScene, new()
    {
        var scene = new T
        {
            LevelIid = iid,
            LoadByGuid = true
        };

        SetNextScene(scene);
        return scene;
    }

    #endregion
}

/// <summary>
///     Typed scene base that exposes a strongly typed logger for the derived scene.
/// </summary>
/// <typeparam name="T">The derived scene type.</typeparam>
public abstract class Scene<T> : Scene where T : class
{
    /// <summary>Strongly-typed logger for the derived scene.</summary>
    protected ILogger Logger { get; private set; } = new Logger<T>();
}

/// <summary>Lifecycle states for <see cref="Scene" />.</summary>
public enum SceneState
{
    Created,
    Initializing,
    Starting,
    Running,
    Ending,
    Disposed
}