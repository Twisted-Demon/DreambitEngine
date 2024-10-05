using System;
using Microsoft.Xna.Framework;
using PixelariaEngine.ECS;
using PixelariaEngine.Graphics;

namespace PixelariaEngine;

public class Scene
{
    public readonly DrawableList Drawables;
    protected readonly EntityList Entities;

    private readonly Logger<Scene> _logger = new();

    public Color BackgroundColor = Color.CornflowerBlue;
    protected bool IsInitialized;
    protected bool IsPaused;
    protected bool IsStarted;
    protected Renderer Renderer;

    public Scene()
    {
        Entities = new EntityList(this);
        Drawables = new DrawableList();
        Renderer = new DefaultRenderer(this);
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
        Renderer?.Initialize();
        MainCamera = Entity.Create("main-camera").AttachComponent<Camera2D>();
        OnInitialize();
    }

    public Scene AddRenderer<T>() where T : Renderer
    {
        var renderer = (T)Activator.CreateInstance(typeof(T), this);
        Renderer = renderer;
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
    internal virtual void OnEnd()
    {
    }

    private void Cleanup()
    {
        Entities.ClearLists();
        Drawables.ClearLists();
        Renderer.CleanUp();
    }

    internal void Terminate()
    {
        OnEnd();
        Cleanup();
    }

    public virtual void Tick()
    {
        if (!IsInitialized)
        {
            InitializeInternals();
            IsInitialized = true;
        }

        UpdateInternals();

        if (!IsStarted)
        {
            _logger.Trace("Scene Starting");
            OnBegin();
            IsStarted = true;
        }

        if (!IsPaused)
            OnUpdate();
    }

    public virtual void OnDraw()
    {
        if (Renderer == null)
        {
            _logger.Error("The Current Scene does not have a Renderer");
            return;
        }

        Renderer.OnDraw();
    }

    public Entity CreateEntity(string name = "entity", string tag = "default", bool enabled = true)
    {
        return Entities.CreateEntity(name, tag, enabled);
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

    public static void SetNextScene(Scene scene)
    {
        Core.Instance.SetNextScene(scene);
    }
}