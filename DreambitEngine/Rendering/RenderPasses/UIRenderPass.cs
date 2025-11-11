using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dreambit.ECS;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class UIRenderPass : RenderPass
{
    public override void Initialize()
    {
        base.Initialize();
        Order = 2;

        Scene.UICamera.SetTargetVerticalResolution(Window.Height);
    }

    private void DrawUIComponents()
    {
        var drawLayers = Scene.Drawables.GetDrawLayers();
        var layerOrder = drawLayers.Keys.OrderBy(x => x).ToList();

        Device.SetRenderTarget(RenderPipeline.SceneRenderTarget);

        for (var i = 0; i < layerOrder.Count; i++)
        {
            Core.SpriteBatch.Begin(transformMatrix: Scene.UICamera.TopLeftTransformMatrix,
                sortMode: SpriteSortMode.Deferred,
                samplerState: Scene.RenderingOptions.UISamplerState,
                blendState: BlendState.AlphaBlend,
                effect: DefaultEffect);

            var drawables = drawLayers[layerOrder[i]]
                .Where(x => x.Enabled && x.Entity.Enabled && x is Canvas);

            foreach (var drawableComponent in drawables)
            {
                var canvas = (Canvas)drawableComponent;
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