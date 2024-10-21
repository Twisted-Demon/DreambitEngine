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
    
    private void DrawUIComponents()
    {
        var drawLayers = Scene.Drawables.GetDrawLayers();
        var layerOrder = drawLayers.Keys.OrderBy(x => x).ToList();

        Device.SetRenderTarget(FinalRenderTarget);
        Device.Clear(Color.Transparent);
        
        for (var i = 0; i < layerOrder.Count; i++)
        {
            Core.SpriteBatch.Begin(transformMatrix: _uiCamera.TopLeftTransformMatrix,
                sortMode: SpriteSortMode.Deferred,
                samplerState: SamplerState.PointClamp,
                blendState: BlendState.AlphaBlend,
                effect: DefaultEffect);

            var drawables = drawLayers[layerOrder[i]]
                .Where(x => x.Enabled && x.Entity.Enabled && x is Canvas);

            foreach (var drawableComponent in drawables)
            {
                var canvas = (Canvas)drawableComponent;
                canvas.UpdateInternalCanvasSize(_targetUIHeight);
                canvas.OnDrawUI();
            }

            Core.SpriteBatch.End();
        }
    }

    public override void OnDraw()
    {
        DrawUIComponents();
    }
    
}