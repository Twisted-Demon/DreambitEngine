using System;
using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public abstract class RenderPass : IDisposable
{
    public int Order = 0;
    
    internal Scene Scene { get; set; }
    protected static GraphicsDevice Device => Core.Instance.GraphicsDevice;
    protected Effect DefaultEffect { get; private set; }
    protected DrawableRepository Drawables => Scene.Drawables;
    
    public RenderPipeline RenderPipeline { get; internal set; }
    
    public bool IsActive { get; set; } = true;

    private bool _isDisposed = false;

    protected virtual void OnWindowResized(object sender, WindowResizedEventArgs args)
    {

    }

    internal void InitializeInternals()
    {
        Window.WindowResized += OnWindowResized;
        DefaultEffect = Resources.LoadAsset<Effect>("Effects/ForwardDiffuse");
        Initialize();
    }

    public virtual void Initialize()
    {
    }

    public virtual void OnDraw()
    {
    }
    
    protected virtual void OnDisposing()
    {
    }

    
    public void Dispose()
    {
        if (_isDisposed) return;
        
        Window.WindowResized -= OnWindowResized;
        Resources.UnloadAsset(DefaultEffect.Name);
        OnDisposing();

        _isDisposed = true;
    }
}