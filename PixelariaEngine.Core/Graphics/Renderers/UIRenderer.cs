using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelariaEngine.ECS;

namespace PixelariaEngine;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class UIRenderer(Scene scene) : Renderer(scene)
{
    private Camera2D _uiCamera;
    private List<RenderTarget2D> _renderTargets = [];
    private int _targetUIHeight => 720;

    public override void Initialize()
    {
        base.Initialize();
        Order = 2;

        _uiCamera = Entity.Create("uiCamera").AttachComponent<Camera2D>();
        _uiCamera.IsFollowing = false;
        _uiCamera.SetTargetVerticalResolution(_targetUIHeight);
        _uiCamera.Zoom = 1.0f;
    }

    private void UpdateRenderTargets()
    {
        if (_renderTargets.Count == Scene.Drawables.DrawLayerCount) return;

        foreach (var renderTarget in _renderTargets)
            renderTarget.Dispose();

        _renderTargets.Clear();

        for (var i = 0; i < Scene.Drawables.DrawLayerCount; i++)
        {
            _renderTargets.Add(CreateRenderTarget());
        }
    }

    private void DrawUIComponents()
    {
        var drawLayers = Scene.Drawables.GetDrawLayers();
        var layerOrder = drawLayers.Keys.OrderBy(x => x).ToList();

        for (var i = 0; i < layerOrder.Count; i++)
        {
            Device.SetRenderTarget(_renderTargets[i]);
            Device.Clear(Color.Transparent);
            
            Core.SpriteBatch.Begin(transformMatrix:_uiCamera.TopLeftTransformMatrix,
                sortMode: SpriteSortMode.Deferred,
                samplerState: SamplerState.PointClamp,
                blendState: BlendState.AlphaBlend,
                effect: DefaultEffect);

            var drawables = drawLayers[layerOrder[i]]
                .Where(x => x.Enabled && x.Entity.Enabled && x is Canvas);
            
            foreach(var drawableComponent in drawables)
            {
                var canvas = (Canvas)drawableComponent;
                canvas.UpdateInternalCanvasSize(_targetUIHeight);
                canvas.OnDrawUI();
            }
            
            Core.SpriteBatch.End();
        }
    }

    private void RenderToFinalRenderTarget()
    {
        Device.SetRenderTarget(FinalRenderTarget);
        Device.Clear(Color.Transparent);
        
        Core.SpriteBatch.Begin(
            
            samplerState: SamplerState.PointClamp, 
            blendState: BlendState.NonPremultiplied);
        
        foreach(var renderTarget in _renderTargets)
            Core.SpriteBatch.Draw(renderTarget, Vector2.Zero, Color.White);
        
        Core.SpriteBatch.End();
    }

    public override void OnDraw()
    {
        UpdateRenderTargets();
        DrawUIComponents();
        RenderToFinalRenderTarget();
    }
    

    protected override void OnWindowResized(object sender, WindowEventArgs args)
    {
        base.OnWindowResized(sender, args);

        if (_uiCamera == null) return;
        
        var renderTargetCount = _renderTargets.Count;
        _uiCamera.SetTargetVerticalResolution(_targetUIHeight);
        
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

    protected override void OnCleanUp()
    {
        foreach(var renderTarget in _renderTargets)
            renderTarget?.Dispose();
        
        _renderTargets.Clear();
        _renderTargets = null;
        _uiCamera = null;
        
    }
    
    
}