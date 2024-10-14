using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine.Graphics;

public class DefaultRenderer(Scene scene) : Renderer(scene)
{
    private List<RenderTarget2D> _layerRenderTargets = [];

    public override void Initialize()
    {
    }

    public override void OnDraw()
    {
        UpdateRenderTargets();
        PreRender();
        RenderDrawables();
        RenderToFinalRenderTarget();
    }

    private void PreRender()
    {
        var drawables = Scene.Drawables.GetAllDrawables()
            .Where(x => x.Enabled && x.Entity.Enabled && x.DrawLayer != RenderLayers.LightLayer);

        foreach (var drawable in drawables)
            drawable.OnPreDraw();
    }

    private void UpdateRenderTargets()
    {
        if (_layerRenderTargets.Count == Scene.Drawables.DrawLayerCount) return;

        foreach (var renderTarget in _layerRenderTargets)
            renderTarget.Dispose();

        _layerRenderTargets.Clear();

        for (var i = 0; i < Scene.Drawables.DrawLayerCount; i++)
        {
            _layerRenderTargets.Add(CreateRenderTarget());
        }
    }

    private void RenderToFinalRenderTarget()
    {
        Device.SetRenderTarget(FinalRenderTarget);
        Device.Clear(Color.Transparent);

        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.NonPremultiplied,
            sortMode: SpriteSortMode.Immediate, effect: DefaultEffect);

        foreach (var renderTarget in _layerRenderTargets)
            Core.SpriteBatch.Draw(renderTarget, Vector2.Zero, Color.White);

        Core.SpriteBatch.End();
        
    }

    private void RenderDrawables()
    {
        var drawLayers = Scene.Drawables.GetDrawLayers();
        var layerOrder = drawLayers.Keys.OrderBy(x => x).ToList();

        for (var i = 0; i < layerOrder.Count; i++)
        {
            //set the render target for the layer
            Device.SetRenderTarget(_layerRenderTargets[i]);
            Device.Clear(Color.Transparent);
            
            Core.SpriteBatch.Begin(transformMatrix: Scene.MainCamera.TransformMatrix,
                samplerState: SamplerState.PointClamp, sortMode: SpriteSortMode.FrontToBack
                , blendState: BlendState.NonPremultiplied);
            
            var drawables = drawLayers[layerOrder[i]]
                .Where(x => x.Enabled && x.Entity.Enabled && x.DrawLayer != RenderLayers.LightLayer);

            foreach (var drawable in drawables
                         .Where(d => d.IsVisibleFromCamera(Scene.MainCamera.Bounds)))
            {
                drawable.OnDraw();
            }
            
            Core.SpriteBatch.End();
        }
        
    }

    protected override void OnWindowResized(object sender, WindowEventArgs args)
    {
        base.OnWindowResized(sender, args);
        
        var renderTargetCount = _layerRenderTargets.Count;
        
        foreach (var renderTarget in _layerRenderTargets)
        {
            renderTarget.Dispose();
        }
        
        _layerRenderTargets.Clear();

        for (var i = 0; i < renderTargetCount; i++)
        {
            _layerRenderTargets.Add(CreateRenderTarget());
        }
    }

    protected override void OnCleanUp()
    {
        foreach(var renderTarget in _layerRenderTargets)
            renderTarget?.Dispose();
        
        _layerRenderTargets.Clear();
        _layerRenderTargets = null;
    }
}