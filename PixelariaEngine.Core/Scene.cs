using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelariaEngine.ECS;
using PixelariaEngine.Graphics;

namespace PixelariaEngine;

public class Scene
{
    private readonly Logger<Scene> _logger = new();

    public bool DebugMode { get; set; }
    public readonly DrawableList Drawables;
    protected readonly EntityList Entities;

    public Color BackgroundColor = Color.CornflowerBlue;
    protected bool IsInitialized;
    protected bool IsPaused;
    protected bool IsStarted;
    protected readonly List<Renderer> Renderers = [];

    private bool _useDefaultRenderer;
    

    public Scene()
    {
        Entities = new EntityList(this);
        Drawables = new DrawableList();

        AddRenderer<DebugRenderer>();
        AddRenderer<UIRenderer>();
        _useDefaultRenderer = true;
    }

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

        if (_useDefaultRenderer)
            AddRenderer<DefaultRenderer>();
        
        foreach(var renderer in Renderers)
            renderer.InitializeInternals();
        
        OnInitialize();
    }

    public Scene AddRenderer<T>() where T : Renderer
    {
        var renderer = (T)Activator.CreateInstance(typeof(T), this);
        Renderers.Add(renderer);
        _useDefaultRenderer = false;
        return this;
    }

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
        foreach(var renderer in Renderers)
            renderer.CleanUpInternal();
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
        if (Renderers.Count == 0)
        {
            _logger.Error("The Current Scene does not have a Renderer");
            return;
        }

        foreach(var renderer in Renderers.OrderBy(x => x.Order).ToList())
            renderer.OnDraw();
        
        Core.Instance.GraphicsDevice.SetRenderTarget(null);
        Core.Instance.GraphicsDevice.Clear(BackgroundColor);
        foreach (var renderer in Renderers.OrderBy(x => x.Order).Where(x => x.IsActive).ToList())
        {
            
            Core.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);

            Core.SpriteBatch.Draw(renderer.FinalRenderTarget, Vector2.Zero, Color.White);
            
            Core.SpriteBatch.End();
        }
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

    public Entity GetEntity(uint id)
    {
        return Entities.GetEntity(id);
    }

    public Entity GetEntity(string name)
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
}

public abstract class Scene<T> : Scene where T : class
{
    protected Logger<T> Logger { get; private set; } = new();
}