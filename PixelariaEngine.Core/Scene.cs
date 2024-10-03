using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelariaEngine.ECS;

namespace PixelariaEngine;

public class Scene
{
    public readonly DrawableList Drawables;
    protected readonly EntityList Entities;
    
    public Color BackgroundColor = Color.CornflowerBlue;
    protected bool IsInitialized;
    protected bool IsPaused;
    protected bool IsStarted;
    
    private SpriteBatch _spriteBatch;

    public Scene()
    {
        Entities = new EntityList(this);
        Drawables = new DrawableList();
    }

    /// <summary>
    ///     Called after the scene has been created, but before actually running.
    ///     Here is where we should load any assets to be used.
    /// </summary>
    protected virtual void OnInitialize()
    {
        
    }

    private void InitializeInternals()
    {
        Log.Debug("Initializing Scene");
        _spriteBatch = new SpriteBatch(Core.Instance.GraphicsDevice);
        OnInitialize();
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
            Log.Trace("Scene Starting");
            OnBegin();
            IsStarted = true;
        }

        OnUpdate();
    }

    public virtual void OnDraw()
    {
        Core.GraphicsDeviceManager.GraphicsDevice.Clear(BackgroundColor);
        
        _spriteBatch.Begin();
        var drawables = Drawables.GetAllDrawables();
        
        foreach(var drawable in drawables)
            drawable.OnDraw(_spriteBatch);
        
        _spriteBatch.End();
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
}