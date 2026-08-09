using System;
using System.Collections.Generic;
using System.Linq;
using Dreambit.ECS;
using Dreambit.Events;
using Dreambit.Scripting;
using Dreambit.UI;
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
    protected Scene()
    {
        Logger = new Logger(GetType());

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

    /// <summary>Logger for this scene.</summary>
    protected readonly ILogger Logger;

    /// <summary>Access to the coroutine system</summary>
    public ICoroutineService CoroutineService => _coroutineScheduler;

    /// <summary>Current scene lifecycle state.</summary>
    public SceneState State { get; internal set; }

    /// <summary>Enables engine-level debug drawing and diagnostics.</summary>
    public bool DebugMode { get; set; }

    /// <summary>Cutscene / script driver for the scene.</summary>
    public readonly ScriptingManager ScriptingManager;

    /// <summary>Clear color used before drawing this scene.</summary>
    public Color BackgroundColor = new(24, 32, 48);

    /// <summary>Post-process configuration shared with passes.</summary>
    public readonly PostProcessSettings PostProcessSettings;

    public readonly RenderingOptions RenderingOptions;

    /// <summary>Primary world camera.</summary>
    public Camera2D MainCamera { get; private set; }

    /// <summary>UI camera for screen-space/UI rendering.</summary>
    public Camera2D UiCamera { get; private set; }

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
        Logger.Debug("Initializing Scene");

        // Create default cameras (world + UI)
        MainCamera = Entity.Create("main-camera").AttachComponent<Camera2D>();
        MainCamera.Entity.AlwaysUpdate = true;

        UiCamera = Entity.Create("ui-camera").AttachComponent<Camera2D>();
        UiCamera.Entity.AlwaysUpdate = true;

        Entity.Create("event-bus").AttachComponent<EventBus>();

        AmbientLight = Entity.Create("ambient-light").AttachComponent<AmbientLight2D>();

        // Setup default render passes
        _renderPipeline.Initialize();
        SetUpRenderPipeLine();
    }

    /// <summary>
    ///     Sets up the default render pass (can be overriden by user)
    /// </summary>
    protected virtual void SetUpRenderPipeLine()
    {
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
        ScriptingManager.Update();
        Entities.Tick();
        _coroutineScheduler.Update();
    }

    /// <summary>
    ///     Routes raw input through UI frames from front to back before gameplay
    ///     action maps and components are updated.
    /// </summary>
    internal void RouteUiInput()
    {
        if (State != SceneState.Running)
            return;

        var frameEntries = Drawables.GetAllUiFrames()
            .Select((frame, index) => (frame, index))
            .ToList();

        foreach (var item in frameEntries)
            if (!item.Item1.Enabled || item.Item1.Entity?.Enabled != true)
                item.Item1.Layout?.ClearInteractionState();

        var frames = frameEntries
            .Where(item =>
                item.Item1.Enabled &&
                item.Item1.Entity?.Enabled == true)
            .OrderByDescending(item =>
                item.Item1.Layout?.IsPointerInputCaptured == true)
            .ThenByDescending(item => item.Item1.DrawLayer)
            .ThenByDescending(item => item.index)
            .ToList();

        var consumed = UiInputCapture.None;
        var focusedFrame = frames
            .Select(item => item.Item1)
            .FirstOrDefault(frame => frame.Layout?.FocusedElement is not null);
        UiFrame pointerPressOwner = null;
        foreach (var item in frames)
        {
            var available = UiInputCapture.All & ~consumed;
            if (focusedFrame is not null &&
                !ReferenceEquals(item.Item1, focusedFrame))
                available &= ~(UiInputCapture.Keyboard | UiInputCapture.GamePad);

            var frameCapture = item.Item1.RouteInput(available);
            consumed |= frameCapture;

            if (pointerPressOwner is null &&
                Input.IsRawMousePressed(MouseButton.Left) &&
                frameCapture.HasFlag(UiInputCapture.Pointer))
                pointerPressOwner = item.Item1;
        }

        if (pointerPressOwner is not null)
            foreach (var item in frames)
                if (!ReferenceEquals(item.Item1, pointerPressOwner))
                    item.Item1.Layout?.ClearFocus();

        Input.CaptureForUi(consumed);
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
        OnEnd();
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
                OnInitialize();
                Transition(SceneState.Starting);
                OnBegin();
                Transition(SceneState.Running);
                break;

            case SceneState.Starting:
            case SceneState.Initializing:
                // Transitional states do not execute per-frame logic.
                break;

            case SceneState.Running:
                UpdateInternals();
                OnUpdate();
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
            OnPhysicsUpdate();
            Entities.PhysicsTick();
            _coroutineScheduler.FixedUpdate();
        }
    }

    /// <summary>
    ///     Draw driver. Calls the render pipeline when running.
    /// </summary>
    public virtual void OnDraw()
    {
        if (State != SceneState.Running) return;

        //Guard.SafeCall(_renderPipeline.OnDraw, "RenderPipeline.OnDraw");
        _renderPipeline.OnDraw();
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
            Logger.Error($"Invalid state transition {State} -> {next}");
            throw new InvalidOperationException($"Invalid state transition {State} -> {next}");
        }

        Logger.Trace($"Scene state: {State} -> {next}");
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
        Vector3? eulerRotation = null,
        Vector3? scale = null,
        Guid? guidOverride = null)
    {
        var entity = Entities.CreateEntity(name, tags, enabled, createAt, eulerRotation, scale, guidOverride);
        return entity;
    }

    public Entity CreateEntity(
        EntityBlueprint blueprint,
        bool? enabled = null,
        Vector3? createAt = null,
        Vector3? eulerRotation = null,
        Vector3? scale = null)
    {
        return SpawnBlueprint(
            blueprint,
            null,
            enabled,
            createAt,
            eulerRotation,
            scale);
    }

    public Entity CreateChildOfEntity(
        EntityBlueprint blueprint,
        Entity parent,
        bool? enabled = null,
        Vector3? createAt = null,
        Vector3? eulerRotation = null,
        Vector3? scale = null)
    {
        ArgumentNullException.ThrowIfNull(parent);

        return SpawnBlueprint(
            blueprint,
            parent,
            enabled,
            createAt,
            eulerRotation,
            scale);
    }

    private Entity SpawnBlueprint(
        EntityBlueprint blueprint,
        Entity parent,
        bool? enabled,
        Vector3? createAt,
        Vector3? eulerRotation,
        Vector3? scale)
    {
        ArgumentNullException.ThrowIfNull(blueprint);

        BlueprintValidator.ValidateOrThrow(blueprint);
        var context = new BlueprintSpawnContext(blueprint);

        try
        {
            var rootEntity = CreateBlueprintHierarchy(
                blueprint,
                parent,
                context,
                true,
                enabled,
                createAt,
                eulerRotation,
                scale);

            foreach (var entityBlueprint in context.Hierarchy)
                context.GetEntity(entityBlueprint.Guid)
                    .BuildComponentsFromBlueprint(entityBlueprint);

            foreach (var entityBlueprint in context.Hierarchy)
                context.GetEntity(entityBlueprint.Guid)
                    .DeserializeComponentsFromBlueprints(entityBlueprint, context);

            foreach (var entityBlueprint in context.Hierarchy)
                context.GetEntity(entityBlueprint.Guid)
                    .CallComponentOnCreateAfterDeserialized();

            return rootEntity;
        }
        catch
        {
            RollbackBlueprintSpawn(context);
            throw;
        }
    }

    private Entity CreateBlueprintHierarchy(
        EntityBlueprint blueprint,
        Entity parent,
        BlueprintSpawnContext context,
        bool isRoot,
        bool? rootEnabled,
        Vector3? rootPosition,
        Vector3? rootRotation,
        Vector3? rootScale)
    {
        var enabled = isRoot && rootEnabled.HasValue
            ? rootEnabled.Value
            : blueprint.Enabled;

        var position = isRoot && rootPosition.HasValue
            ? rootPosition.Value
            : blueprint.Position;

        var rotation = isRoot && rootRotation.HasValue
            ? rootRotation.Value
            : blueprint.Rotation;

        var scale = isRoot && rootScale.HasValue
            ? rootScale.Value
            : blueprint.Scale;

        var entity = Entities.CreateEntity(
            blueprint.Name,
            blueprint.Tags,
            enabled,
            position,
            rotation,
            scale);

        if (parent != null)
            entity.Parent = parent;

        context.Register(blueprint, entity);

        foreach (var childBlueprint in blueprint.Children)
            CreateBlueprintHierarchy(
                childBlueprint,
                entity,
                context,
                false,
                null,
                null,
                null,
                null);

        return entity;
    }

    private void RollbackBlueprintSpawn(BlueprintSpawnContext context)
    {
        for (var i = context.Hierarchy.Count - 1; i >= 0; i--)
        {
            var blueprint = context.Hierarchy[i];
            if (!context.TryGetEntity(blueprint.Guid, out var entity))
                continue;

            entity.Parent = null;
            Entities.DestroyEntityImmediately(entity);
        }
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
