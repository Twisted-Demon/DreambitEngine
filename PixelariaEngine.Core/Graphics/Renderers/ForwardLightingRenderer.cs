using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelariaEngine.ECS;

namespace PixelariaEngine;

public class ForwardLightingRenderer(Scene scene) : Renderer(scene)
{
    private List<RenderTarget2D> _layerRenderTargets = [];
    private Effect _forwardLightingEffect;

    public override void Initialize()
    {
        _forwardLightingEffect = Resources.LoadAsset<Effect>("Effects/ForwardLighting");
    }

    public override void OnDraw()
    {
        UpdateRenderTargets();
        PreRender();
        //RenderDrawables();
        RenderToFinalRenderTarget();
    }

    private void PreRender()
    {
        var drawables = Scene.Drawables.GetAllDrawables()
            .Where(x => x.Enabled && x.Entity.Enabled && x.DrawLayer != RenderLayers.LightLayer);
//
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
            sortMode: SpriteSortMode.Immediate);
        
        foreach (var renderTarget in _layerRenderTargets)
            Core.SpriteBatch.Draw(renderTarget, Vector2.Zero, Color.White);

        Core.SpriteBatch.End();
    }

    //private void RenderDrawables()
    //{
    //    var drawLayers = Scene.Drawables.GetDrawLayers();
    //    var layerOrder = drawLayers.Keys.OrderBy(l => l).ToList();
//
    //    var lights = Scene.Drawables.GetDrawablesByDrawLayer(RenderLayers.LightLayer)
    //        .Select(x => x as Light2D).ToArray();
    //    
    //    //set the lighting parameters
    //    _forwardLightingEffect.Parameters["LightCount"].SetValue(lights.Length);
    //    
    //    //prepare the light data
    //    float[] lightData = new float[lights.Length * 7];
    //    for (int i = 0; i < lights.Length; i++)
    //    {
    //        int index = (i * 7);
    //        var worldPos = lights[i].Position;
    //        var screenPos = Vector2.Transform(worldPos, Scene.MainCamera.TransformMatrix);
    //        
    //        // position data
    //        lightData[index] = screenPos.X;
    //        lightData[index + 1] = screenPos.Y;
    //        
    //        // Color data
    //        lightData[index + 2] = lights[i].Color.X;
    //        lightData[index + 3] = lights[i].Color.Y;
    //        lightData[index + 4] = lights[i].Color.Z;
    //        
    //        //Intensity and Radius
    //        lightData[index + 5] = lights[i].Intensity;
    //        lightData[index + 6] = lights[i].Radius;
    //    }
    //    
    //    
    //    _forwardLightingEffect.Parameters["Lights"].SetValue(lightData);
//
    //    for (var i = 0; i < layerOrder.Count; i++)
    //    {
    //        //set the render target for the layer
    //        Device.SetRenderTarget(_layerRenderTargets[i]);
    //        Device.Clear(Color.Transparent);
    //        
    //        Core.SpriteBatch.Begin(transformMatrix: Scene.MainCamera.TransformMatrix,
    //            samplerState: SamplerState.PointClamp, sortMode: SpriteSortMode.FrontToBack
    //            , blendState: BlendState.AlphaBlend, effect: _forwardLightingEffect);
    //        
    //        var drawables = drawLayers[layerOrder[i]]
    //            .Where(x => x.Enabled && x.Entity.Enabled && x.DrawLayer != RenderLayers.LightLayer);
    //        
    //        foreach (var drawable in drawables)
    //            drawable.OnDraw();
    //        
    //        Core.SpriteBatch.End();
    //    }
    //}
    
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