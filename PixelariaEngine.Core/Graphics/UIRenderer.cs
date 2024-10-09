using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelariaEngine.ECS;
using PixelariaEngine.ECS.UI;

namespace PixelariaEngine.Graphics;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class UIRenderer(Scene scene) : Renderer(scene)
{
    private Camera2D _uiCamera;
    private readonly List<RenderTarget2D> _renderTargets = [];

    public override void Initialize()
    {
        base.Initialize();
        Order = 2;

        _uiCamera = Entity.Create("uiCamera").AttachComponent<Camera2D>();
        _uiCamera.IsFollowing = false;
        _uiCamera.SetTargetVerticalResolution(768);
        _uiCamera.Zoom = 1.0f;
    }

    private void UpdateRenderTargets()
    {
        //if the layer count has changed
        if (_renderTargets.Count == Scene.Drawables.DrawLayerCount) return;
        
        ClearRenderTargets();
    
        //rebuild our render targets
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

            Core.SpriteBatch.Begin(transformMatrix: _uiCamera.TransformMatrix,
                samplerState: SamplerState.PointClamp,
                blendState: BlendState.NonPremultiplied);

            var drawables = drawLayers[layerOrder[i]]
                .Where(x => x.Enabled && x.Entity.Enabled && x is UIComponent);
            
            foreach(var drawableComponent in drawables)
            {
                var uiComponent = (UIComponent)drawableComponent;
                uiComponent.OnDrawUI();
            }
            
            Core.SpriteBatch.End();
        }
    }

    public override void OnDraw()
    {
        UpdateRenderTargets();
        DrawUIComponents();
    }

    private void ClearRenderTargets()
    {
        //clear the render targets and dispose of them
        foreach (var renderTarget in _renderTargets)
        {
            renderTarget.Dispose();
        }
        
        _renderTargets.Clear();
    }

    protected override void OnWindowResized(object sender, WindowEventArgs args)
    {
        ClearRenderTargets();

        for (var i = 0; i < _renderTargets.Count; i++)
        {
            _renderTargets.Add(CreateRenderTarget());
        }
    }
}