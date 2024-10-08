using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine.Graphics;

public class DefaultRenderer(Scene scene) : Renderer(scene)
{
    private readonly List<RenderTarget2D> _renderTargets = [];

    public override void Initialize()
    {
    }

    public override void OnDraw()
    {
        UpdateRenderTargets();
        PreRender();
        RenderDrawables();
        ShowRenderBuffer();
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
        if (_renderTargets.Count == Scene.Drawables.DrawLayerCount) return;

        foreach (var renderTarget in _renderTargets)
        {
            renderTarget.Dispose();
        }

        _renderTargets.Clear();

        for (var i = 0; i < Scene.Drawables.DrawLayerCount; i++)
        {
            _renderTargets.Add(CreateRenderTarget());
        }
    }

    private void ShowRenderBuffer()
    {
        Device.SetRenderTarget(null);
        Device.Clear(Scene.BackgroundColor);

        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.NonPremultiplied,
            sortMode: SpriteSortMode.FrontToBack);

        foreach (var renderTarget in _renderTargets)
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
            Device.SetRenderTarget(_renderTargets[i]);
            
            Device.Clear(Color.Transparent);
            
            Core.SpriteBatch.Begin(transformMatrix: Scene.MainCamera.TransformMatrix,
                samplerState: SamplerState.PointClamp, sortMode: SpriteSortMode.FrontToBack
                , blendState: BlendState.NonPremultiplied);
            
            var drawables = drawLayers[layerOrder[i]]
                .Where(x => x.Enabled && x.Entity.Enabled && x.DrawLayer != RenderLayers.LightLayer);
            
            foreach (var drawable in drawables)
                drawable.OnDraw();
            
            Core.SpriteBatch.End();
        }
        
    }

    protected override void OnWindowResized(object sender, WindowEventArgs args)
    {
        var renderTargetCount = _renderTargets.Count;
        
        foreach (var renderTarget in _renderTargets)
        {
            renderTarget.Dispose();
        }
        
        _renderTargets.Clear();

        for (var i = 0; i < renderTargetCount; i++)
        {
            _renderTargets.Add(CreateRenderTarget());
        }
    }
    
}