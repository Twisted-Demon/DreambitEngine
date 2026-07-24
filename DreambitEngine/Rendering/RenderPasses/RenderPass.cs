using System;
using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public abstract class RenderPass : IDisposable
{
    private bool _isDisposed;
    public int Order = 0;

    internal Scene Scene { get; init; }
    protected static GraphicsDevice Device => Core.Instance.GraphicsDevice;
    protected Effect DefaultEffect { get; private set; }
    protected DrawableRepository Drawables => Scene.Drawables;

    public RenderPipeline RenderPipeline { get; internal init; }

    public bool IsActive { get; set; } = true;

    protected void ApplySpriteBatchMatrix(Effect effect, Matrix transformMatrix)
    {
        if (effect == null) return;

        var matrixParam = effect.Parameters["MatrixTransform"];

        if (matrixParam == null)
            return;
        
        var viewport = Device.Viewport;
        
        var projection = Matrix.CreateOrthographicOffCenter(
            left: 0f,
            right: viewport.Width,
            bottom: viewport.Height,
            top: 0f,
            zNearPlane: 0f,
            zFarPlane: -1f
        );

        matrixParam.SetValue(transformMatrix * projection);
    }
    
    public void Dispose()
    {
        if (_isDisposed) return;

        Window.WindowResized -= OnWindowResized;
        Resources.UnloadAsset(DefaultEffect.Name);
        OnDisposing();

        _isDisposed = true;
        
        GC.SuppressFinalize(this);
    }

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
}