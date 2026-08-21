using System;
using System.Collections.Generic;
using System.Linq;
using Dreambit.ECS;
using Dreambit.Events;
using Dreambit.LDtk;
using Dreambit.Scripting;
using Dreambit.Tiled;
using Dreambit.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
    protected Scene(SceneExecutionMode executionMode = SceneExecutionMode.Runtime)
    {
        ExecutionMode = executionMode;
        Logger = new Logger(GetType());

        Entities = new EntityRepository(this);
        Drawables = new DrawableRepository();
        Services = new SceneServiceCollection(this);
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
        if (_isDisposed || _isDisposing) return;
        _isDisposing = true;

        if (State != SceneState.Ending)
            Transition(SceneState.Ending);

        try
        {
            if (ExecutionMode == SceneExecutionMode.Runtime && _hasBegun && !_hasEnded)
            {
                _hasEnded = true;
                OnEnd();
            }
        }
        catch (Exception exception)
        {
            Logger.Error($"Scene OnEnd failed: {exception}");
        }
        finally
        {
            Transition(SceneState.Disposed);
            try
            {
                Cleanup();
            }
            finally
            {
                _isDisposed = true;
                _isDisposing = false;
                GC.SuppressFinalize(this);
            }
        }
    }

    #endregion

    #region Cutscene Helpers

    /// <summary>
    ///     Loads and starts a cutscene asset by name using the scene's ScriptingManager.
    /// </summary>
    public static bool StartCutscene(string assetName)
    {
        return Core.Instance.CurrentScene.ScriptingManager.StartCutscene(assetName);
    }

    /// <summary>
    ///     Starts an already-loaded cutscene asset.
    /// </summary>
    public static bool StartCutscene(Cutscene cutscene)
    {
        return Core.Instance.CurrentScene.ScriptingManager.StartCutscene(cutscene);
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

    public static void SetNextScene(string sceneAssetName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneAssetName);
        var blueprint = Resources.LoadAsset<SceneBlueprint>(sceneAssetName)
                        ?? throw new InvalidOperationException(
                            $"Scene asset '{sceneAssetName}' could not be loaded.");

        var scene = new Scene();
        scene.LoadIntoSelf(blueprint);
        SetNextScene(scene);
    }

    public static void SetNextScene<T>(string sceneAssetName) where T : Scene, new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneAssetName);
        var blueprint = Resources.LoadAsset<SceneBlueprint>(sceneAssetName)
                        ?? throw new InvalidOperationException(
                            $"Scene asset '{sceneAssetName}' could not be loaded.");

        var scene = new T();
        scene.LoadIntoSelf(blueprint);
        SetNextScene(scene);
    }

    public void LoadIntoSelf(SceneBlueprint blueprint)
    {
        LoadIntoSelf(blueprint, SceneBlueprintLoadOptions.Runtime);
    }

    /// <summary>Loads a baked scene asset and materializes it into this scene.</summary>
    public void LoadIntoSelf(string sceneAssetName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneAssetName);
        var blueprint = Resources.LoadAsset<SceneBlueprint>(sceneAssetName)
                        ?? throw new InvalidOperationException(
                            $"Scene asset '{sceneAssetName}' could not be loaded.");
        LoadIntoSelf(blueprint);
    }

    public void LoadIntoSelf(SceneBlueprint blueprint, SceneBlueprintLoadOptions options)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(options);

        if (blueprint.LDtk is { } ldtk)
            MaterializeLDtkScene(ldtk, options);

        if (blueprint.Tiled is { } tiled)
            MaterializeTiledScene(tiled, options);

        if (options.ApplySceneSettings)
            ApplySettings(blueprint.Settings);

        if (blueprint.Entities.Count == 0)
            return;

        var materializedRoots = BlueprintInstanceMaterializer.Materialize(
            blueprint.Entities,
            options.BlueprintInstanceResolver ?? ResolveBlueprintInstance);

        if (!options.AllowMissingComponentTypes)
        {
            var validationRoot = new EntityBlueprint
            {
                Name = string.IsNullOrWhiteSpace(blueprint.Name) ? "scene" : blueprint.Name,
                Children = materializedRoots.ToList()
            };
            BlueprintValidator.ValidateOrThrow(validationRoot);
        }

        var context = new BlueprintSpawnContext(materializedRoots);
        try
        {
            foreach (var root in materializedRoots)
                CreateBlueprintHierarchy(
                    root,
                    null,
                    context,
                    true,
                    null,
                    null,
                    null,
                    null,
                    options.PreserveEntityIds);

            BuildBlueprintComponents(context, options.TolerateComponentLoadErrors);
        }
        catch
        {
            RollbackBlueprintSpawn(context);
            throw;
        }
    }

    #endregion

    #region Fields (Internals)

    /// <summary>Drawables repository for render-ordered components.</summary>
    internal readonly DrawableRepository Drawables;

    /// <summary>Entities repository for ECS management.</summary>
    internal readonly EntityRepository Entities;

    /// <summary>Render pipeline composed of render passes.</summary>
    private RenderPipeline _renderPipeline;

    private bool _renderPipelineInitialized;

    /// <summary>Tracks disposal state to avoid double-dispose.</summary>
    private bool _isDisposed;

    private bool _isDisposing;

    private bool _hasBegun;
    private bool _hasEnded;

    private readonly CoroutineScheduler _coroutineScheduler;

    #endregion

    #region Public Members & Properties

    /// <summary>Convenience access to the active scene from the core.</summary>
    public static Scene Instance => Core.Instance.CurrentScene;

    /// <summary>Logger for this scene.</summary>
    protected internal readonly ILogger Logger;

    /// <summary>Access to the coroutine system</summary>
    public ICoroutineService CoroutineService => _coroutineScheduler;

    /// <summary>Current scene lifecycle state.</summary>
    public SceneState State { get; internal set; }

    /// <summary>Whether this scene is executing gameplay or hosted for authoring.</summary>
    public SceneExecutionMode ExecutionMode { get; }

    /// <summary>Component-backed services owned by this scene.</summary>
    public SceneServiceCollection Services { get; }

    /// <summary>Enables engine-level debug drawing and diagnostics.</summary>
    public bool DebugMode { get; set; }

    /// <summary>Cutscene / script driver for the scene.</summary>
    public readonly ScriptingManager ScriptingManager;

    /// <summary>Clear color used before drawing this scene.</summary>
    public Color BackgroundColor = new(24, 32, 48);

    /// <summary>Post-process configuration shared with passes.</summary>
    public readonly PostProcessSettings PostProcessSettings;

    /// <summary>Authorable rendering settings currently applied to this scene.</summary>
    public readonly SceneSettings Settings = new();

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
        ApplyAmbientLightSettings();

        EnsureRenderPipelineInitialized(new Point(Window.Width, Window.Height));
    }

    /// <summary>
    ///     Sets up the default render pass (can be overriden by user)
    /// </summary>
    protected virtual void SetUpRenderPipeLine()
    {
        AddRenderPass<SortDrawablesPass>();
        AddRenderPass<AlbedoPass>();
        AddRenderPass<DepthPass>();
        AddRenderPass<DepthLightingPass>();
        AddRenderPass<BloomPass>();
        AddRenderPass<PostProcessRenderPass>();
        if (ExecutionMode == SceneExecutionMode.Runtime)
        {
            AddRenderPass<DebugRenderPass>();
            AddRenderPass<UIRenderPass>();
        }
    }

    private void EnsureRenderPipelineInitialized(
        Point viewportSize)
    {
        if (_renderPipelineInitialized)
            return;

        try
        {
            _renderPipeline.Initialize(
                viewportSize);

            SetUpRenderPipeLine();

            _renderPipelineInitialized = true;
        }
        catch (Exception initializationException)
        {
            // Remove the failed pipeline from Scene ownership immediately.
            var failedPipeline =
                _renderPipeline;

            _renderPipeline =
                new RenderPipeline(this);

            _renderPipelineInitialized =
                false;

            try
            {
                failedPipeline?.Dispose();
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException(
                    "Scene render pipeline initialization failed and cleanup also failed.",
                    new[]
                    {
                        initializationException,
                        cleanupException
                    });
            }

            throw;
        }
    }

    protected void AddRenderPass<T>() where T : RenderPass, new()
    {
        _renderPipeline.AddRenderPass<T>();
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
        var cleanupErrors =
            new List<Exception>();

        TryCleanup(
            cleanupErrors,
            _coroutineScheduler.StopAllCoroutines);

        TryCleanup(
            cleanupErrors,
            ScriptingManager.CleanUp);

        TryCleanup(
            cleanupErrors,
            Entities.ClearLists);

        TryCleanup(
            cleanupErrors,
            Drawables.ClearLists);

        // Break Scene -> RenderPipeline ownership before invoking potentially
        // user-defined pass cleanup.
        var renderPipeline =
            _renderPipeline;

        _renderPipeline = null;
        _renderPipelineInitialized = false;

        if (renderPipeline is not null)
        {
            TryCleanup(
                cleanupErrors,
                renderPipeline.Dispose);
        }

        // Do not keep disposed components alive through convenience fields.
        MainCamera = null;
        UiCamera = null;
        AmbientLight = null;

        if (cleanupErrors.Count > 0)
        {
            throw new AggregateException(
                "One or more resources failed while disposing the scene.",
                cleanupErrors);
        }

        static void TryCleanup(
            List<Exception> errors,
            Action cleanup)
        {
            try
            {
                cleanup();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }
    }

    #endregion

    #region External Lifecycle Control (Engine calls)

    /// <summary>
    ///     Requests scene termination and transitions to ending state.
    /// </summary>
    internal void Terminate()
    {
        if (_isDisposed || State == SceneState.Disposed) return;
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
                Services.ActivateAll();
                Transition(SceneState.Starting);
                _hasBegun = true;
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
    /// Applies queued entity/component additions and removals without running gameplay.
    /// </summary>
    public void FlushStructuralChanges()
    {
        if (_isDisposed) return;
        Entities.FlushStructuralChanges();
    }

    /// <summary>Applies serialized scene rendering settings without replacing the scene.</summary>
    public void ApplySettings(SceneSettings? settings)
    {
        settings ??= new SceneSettings();
        Settings.AmbientLightIntensity = settings.AmbientLightIntensity;
        Settings.AmbientLightColor = settings.AmbientLightColor;
        Settings.PostProcessing = settings.PostProcessing?.Clone() ?? new PostProcessSettings();
        Settings.Exposure = settings.Exposure;

        PostProcessSettings.HueShift = Settings.PostProcessing.HueShift;
        PostProcessSettings.Saturation = Settings.PostProcessing.Saturation;
        PostProcessSettings.TintColor = Settings.PostProcessing.TintColor;
        PostProcessSettings.ToneMappingType = Settings.PostProcessing.ToneMappingType;
        PostProcessSettings.BloomEnabled = Settings.PostProcessing.BloomEnabled;
        PostProcessSettings.BloomIntensity = Settings.PostProcessing.BloomIntensity;
        PostProcessSettings.BloomThreshold = Settings.PostProcessing.BloomThreshold;
        PostProcessSettings.BloomSoftKnee =  Settings.PostProcessing.BloomSoftKnee;
        ApplyAmbientLightSettings();
    }

    /// <summary>Runs opt-in editor callbacks while keeping gameplay lifecycle suppressed.</summary>
    public void EditorTick()
    {
        if (_isDisposed) return;
        if (ExecutionMode != SceneExecutionMode.Editor)
            throw new InvalidOperationException("EditorTick is only valid for editor-hosted scenes.");

        Entities.EditorUpdate();
    }

    /// <summary>Creates or returns the transient camera used by editor-hosted rendering.</summary>
    public Camera2D EnsureEditorCamera()
    {
        if (ExecutionMode != SceneExecutionMode.Editor)
            throw new InvalidOperationException("The transient editor camera is only valid in editor scenes.");
        if (MainCamera is not null)
            return MainCamera;

        var cameraEntity = CreateEntity("__dreambit-editor-camera");
        cameraEntity.IsEditorOnly = true;
        MainCamera = cameraEntity.AttachComponent<Camera2D>();
        FlushStructuralChanges();
        return MainCamera;
    }

    private void ApplyAmbientLightSettings()
    {
        if (AmbientLight is null)
            return;

        AmbientLight.Intensity = Settings.AmbientLightIntensity;
        AmbientLight.Color = Settings.AmbientLightColor;
    }

    /// <summary>Invokes component editor gizmos without running gameplay drawing callbacks.</summary>
    public void DrawEditorGizmos(
        IEditorGizmoContext context,
        IReadOnlySet<Guid> selectedEntityIds)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(selectedEntityIds);
        if (ExecutionMode != SceneExecutionMode.Editor)
            throw new InvalidOperationException("Editor gizmos require an editor-hosted scene.");

        foreach (var entity in GetAllEntities())
        {
            if ((entity.IsEditorOnly && !entity.IsImportedMapGenerated) || !entity.Enabled)
                continue;
            var selected = selectedEntityIds.Contains(entity.Id);
            foreach (var component in entity.GetAllAttachedComponents())
                component.EditorDrawGizmos(context, selected);
        }
    }

    private static EntityBlueprint ResolveBlueprintInstance(BlueprintInstanceReference instance)
    {
        object resolved = instance.AssetId != Guid.Empty
            ? Resources.LoadDreambitAsset(
                new AssetId(instance.AssetId),
                instance.AssetName,
                typeof(EntityBlueprint))
            : Resources.LoadDreambitAsset(instance.AssetName, typeof(EntityBlueprint));
        return resolved as EntityBlueprint
               ?? throw new InvalidOperationException(
                   $"Blueprint asset '{instance.AssetName}' could not be loaded.");
    }

    private void MaterializeLDtkScene(
        LDtkSceneReference reference,
        SceneBlueprintLoadOptions options)
    {
        var project = (options.LDtkProjectResolver ?? ResolveLDtkProject)(reference)
                      ?? throw new InvalidOperationException(
                          $"LDtk project asset '{reference.AssetName}' could not be loaded.");
        var world = reference.WorldIid == Guid.Empty
            ? project.LoadWorld()
            : project.LoadWorld(reference.WorldIid);
        var importer = new LDtkLevelImporter();
        var importOptions = (reference.ImportOptions ?? new LDtkImportOptions()).Clone();
        importOptions.Validate();

        foreach (var levelStub in world.Levels)
        {
            var level = world.LoadLevel(levelStub.Iid);
            var instance = importer.Import(this, world, level, importOptions);
            if (options.MaterializeLDtkEntities)
                LDtkSceneEntityMaterializer.Materialize(this, instance, instance.EntityInstances);
            LDtkGeneratedEntityOverrides.Apply(
                instance.OwnedEntities,
                reference.EntityOverrides ?? new Dictionary<string, LDtkGeneratedEntityOverride>());
            if (!options.MarkImportedLDtkEntitiesEditorOnly)
                continue;
            foreach (var entity in instance.OwnedEntities)
                entity.IsEditorOnly = true;
        }
    }

    private static LDtkFile ResolveLDtkProject(LDtkSceneReference reference)
    {
        var assetName = reference.AssetName;
        if (reference.AssetId != Guid.Empty &&
            Resources.AssetRegistry?.TryResolveAssetName(
                new AssetId(reference.AssetId),
                out var resolvedName) == true)
        {
            assetName = resolvedName;
        }

        return string.IsNullOrWhiteSpace(assetName)
            ? null
            : Resources.LoadAsset<LDtkFile>(assetName);
    }

    private void MaterializeTiledScene(
        TiledSceneReference reference,
        SceneBlueprintLoadOptions options)
    {
        var map = (options.TiledMapResolver ?? ResolveTiledMap)(reference)
                  ?? throw new InvalidOperationException(
                      $"Tiled map asset '{reference.AssetName}' could not be loaded.");
        var importer = new TiledMapImporter();
        var importOptions = (reference.ImportOptions ?? new TiledImportOptions()).Clone();
        importOptions.Validate();
        var instance = importer.Import(this, map, importOptions);
        TiledGeneratedEntityOverrides.Apply(
            instance.OwnedEntities,
            reference.EntityOverrides ?? new Dictionary<string, TiledGeneratedEntityOverride>());
        if (!options.MarkImportedTiledEntitiesEditorOnly)
            return;
        foreach (var entity in instance.OwnedEntities)
            entity.IsEditorOnly = true;
    }

    private static TmxMap ResolveTiledMap(TiledSceneReference reference)
    {
        var assetName = reference.AssetName;
        if (reference.AssetId != Guid.Empty &&
            Resources.AssetRegistry?.TryResolveAssetName(
                new AssetId(reference.AssetId),
                out var resolvedName) == true)
        {
            assetName = resolvedName;
        }

        return string.IsNullOrWhiteSpace(assetName)
            ? null
            : Resources.LoadAsset<TmxMap>(assetName);
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

    /// <summary>
    /// Renders this scene through its normal world and post-process passes into a
    /// caller-owned target without running gameplay lifecycle or backbuffer UI passes.
    /// </summary>
    public void RenderTo(RenderTarget2D target, Camera2D camera)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(camera);
        if (target.IsDisposed)
            throw new ObjectDisposedException(nameof(target));
        if (camera.Scene != this)
            throw new ArgumentException("The render camera must belong to this scene.", nameof(camera));

        var viewportSize = new Point(target.Width, target.Height);
        EnsureRenderPipelineInitialized(viewportSize);
        _renderPipeline.Render(camera, target, viewportSize, false);
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
            (SceneState.Created, SceneState.Ending) => true,
            (SceneState.Initializing, SceneState.Starting) => true,
            (SceneState.Initializing, SceneState.Ending) => true,
            (SceneState.Starting, SceneState.Running) => true,
            (SceneState.Starting, SceneState.Ending) => true,
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
                scale,
                false);

            BuildBlueprintComponents(context, false);

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
        Vector3? rootScale,
        bool preserveEntityIds)
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
            scale,
            preserveEntityIds ? blueprint.Guid : null);

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
                null,
                preserveEntityIds);

        return entity;
    }

    private static void BuildBlueprintComponents(
        BlueprintSpawnContext context,
        bool tolerateComponentLoadErrors)
    {
        foreach (var entityBlueprint in context.Hierarchy)
            context.GetEntity(entityBlueprint.Guid)
                .BuildComponentsFromBlueprint(entityBlueprint, tolerateComponentLoadErrors);

        foreach (var entityBlueprint in context.Hierarchy)
            context.GetEntity(entityBlueprint.Guid)
                .DeserializeComponentsFromBlueprints(
                    entityBlueprint,
                    context,
                    tolerateComponentLoadErrors);

        foreach (var entityBlueprint in context.Hierarchy)
            context.GetEntity(entityBlueprint.Guid)
                .CallComponentOnCreateAfterDeserialized();
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

    /// <summary>Returns active and not-yet-flushed entities, including disabled entities.</summary>
    public IReadOnlyList<Entity> GetAllEntities()
    {
        return Entities.GetAllEntities();
    }

    /// <summary>Returns drawable components registered with this scene.</summary>
    public IReadOnlyList<DrawableComponent> GetAllDrawables()
    {
        return Drawables.GetAllDrawables();
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
