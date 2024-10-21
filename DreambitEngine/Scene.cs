using System;
using System.Collections.Generic;
using System.Linq;
using Dreambit.ECS;
using Dreambit.Graphics;
using Dreambit.Scripting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public class Scene
{
    private readonly Logger<Scene> _logger = new();

    public readonly ScriptingManager ScriptingManager;

    internal readonly DrawableList Drawables;
    internal readonly EntityList Entities;

    private bool _useDefaultRenderer;
    protected bool IsInitialized;
    protected bool IsStarted;
    protected bool IsPaused;
    
    public Color BackgroundColor = Color.CornflowerBlue;

    private Renderer _sceneRenderer;
    private readonly DebugRenderer _debugRenderer;
    private readonly UIRenderer _uiRenderer;
    private readonly PostProcessRenderer _postProcessRenderer;
    
    public Scene()
    {
        Entities = new EntityList(this);
        Drawables = new DrawableList();
        ScriptingManager = new ScriptingManager();
        
        _useDefaultRenderer = true;

        _sceneRenderer = new DefaultRenderer(this);
        _debugRenderer = new DebugRenderer(this);
        _uiRenderer = new UIRenderer(this);
        _postProcessRenderer = new PostProcessRenderer(this, _sceneRenderer);
    }

    public bool DebugMode { get; set; }

    public static Scene Instance => Core.Instance.CurrentScene;

    public Camera2D MainCamera { get; private set; }

    /// <summary>
    ///     Called after the scene has been created, but before actually running.
    ///     Here is where we should load any assets to be used.
    /// </summary>
    protected virtual void OnInitialize()
    {
    }

    private void InitializeInternals()
    {
        _logger.Debug("Initializing Scene");
        MainCamera = Entity.Create("main-camera").AttachComponent<Camera2D>();
        MainCamera.Entity.AlwaysUpdate = true;
        
        _sceneRenderer.InitializeInternals();
        _debugRenderer.InitializeInternals();
        _uiRenderer.InitializeInternals();
        _postProcessRenderer.InitializeInternals();

        OnInitialize();
    }

    //public Scene SetRenderer<T>() where T : Renderer
    //{
    //    _sceneRenderer = (T)Activator.CreateInstance(typeof(T), this);
    //    _useDefaultRenderer = false;
    //    return this;
    //}

    /// <summary>
    ///     Called after the scene has been initialized and has actually begun.
    ///     Internals have already been pre-initialized and updated.
    /// </summary>
    protected virtual void OnBegin()
    {
    }

    /// <summary>
    ///     Called every frame
    /// </summary>
    protected virtual void OnUpdate()
    {
    }

    private void UpdateInternals()
    {
        ScriptingManager.Update();
        Entities.OnTick();
    }

    /// <summary>
    ///     Called when a scene is ended.
    /// </summary>
    protected virtual void OnEnd()
    {
    }

    private void Cleanup()
    {
        Entities.ClearLists();
        Drawables.ClearLists();
        
        _sceneRenderer.CleanUpInternal();
        _debugRenderer.CleanUpInternal();
        _uiRenderer.CleanUpInternal();
        _postProcessRenderer.CleanUpInternal();
    }

    internal void Terminate()
    {
        OnEnd();
        Cleanup();

        // Force garbage collection
        GC.Collect();

        // Wait for finalizers to run
        GC.WaitForPendingFinalizers();

        // Optionally, force another GC collection to clean up finalized objects
        GC.Collect();
    }

    public virtual void Tick()
    {
        if (!IsInitialized)
        {
            InitializeInternals();
            IsInitialized = true;
        }

        if (!IsStarted)
        {
            _logger.Trace("Scene Starting");
            OnBegin();
            IsStarted = true;
        }

        UpdateInternals();

        //TODO: fix pausing
        if (!IsPaused)
            OnUpdate();
    }

    public virtual void OnDraw()
    {
        // rune the draw methods of the renderers
        _sceneRenderer.OnDraw();
        _debugRenderer.OnDraw();
        _uiRenderer.OnDraw();
        _postProcessRenderer.OnDraw();

        // draw to the screen
        Core.Instance.GraphicsDevice.SetRenderTarget(null);
        Core.Instance.GraphicsDevice.Clear(BackgroundColor);
        
        Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        
        Core.SpriteBatch.Draw(_postProcessRenderer.FinalRenderTarget, Vector2.Zero, Color.White);
        Core.SpriteBatch.Draw(_debugRenderer.FinalRenderTarget, Vector2.Zero, Color.White);
        Core.SpriteBatch.Draw(_uiRenderer.FinalRenderTarget, Vector2.Zero, Color.White);
        
        Core.SpriteBatch.End();

    }

    public Entity CreateEntity(string name = "entity", HashSet<string> tags = null
        , bool enabled = true, Vector3? createAt = null)
    {
        var entity = Entities.CreateEntity(name, tags, enabled, createAt);
        return entity;
    }

    public void SetEntityAlwaysUpdate(Entity entity, bool value)
    {
        Entities.SetEntityAlwaysUpdate(entity, value);
    }

    public void DestroyEntity(Entity entity)
    {
        Entities.DestroyEntity(entity);
    }

    public Entity FindEntity(uint id)
    {
        return Entities.GetEntity(id);
    }

    public Entity FindEntity(string name)
    {
        return Entities.GetEntity(name);
    }

    public List<Entity> GetAllActiveEntities()
    {
        return Entities.GetAllActiveEntitiesEntities();
    }

    public static void SetNextScene(Scene scene)
    {
        Core.Instance.SetNextScene(scene);
    }

    public static LDtkScene SetNextLDtkScene(string identifier)
    {
        var scene = new LDtkScene();
        scene.LevelIdentifier = identifier;
        SetNextScene(scene);

        return scene;
    }

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

    public static void StartCutscene(string cutsceneName, string fileExtension = ".yaml")
    {
        Core.Instance.CurrentScene.ScriptingManager.StartCutscene(cutsceneName, fileExtension);
    }
}

public abstract class Scene<T> : Scene where T : class
{
    protected Logger<T> Logger { get; private set; } = new();
}