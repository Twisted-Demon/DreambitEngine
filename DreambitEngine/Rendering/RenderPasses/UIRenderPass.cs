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

        
    }

    private void DrawUIComponents()
    {
        var drawLayers = Scene.Drawables.GetDrawLayers();
        var layerOrder = drawLayers.Keys.OrderBy(x => x).ToList();
        Scene.UICamera.SetTargetVerticalResolution(Window.Height);

        Device.SetRenderTarget(RenderPipeline.SceneRenderTarget);
        
        for (var i = 0; i < layerOrder.Count; i++)
        {
            Core.SpriteBatch.Begin(
                transformMatrix: Scene.UICamera.TopLeftTransformMatrix,
                sortMode: SpriteSortMode.Deferred,
                samplerState: Scene.RenderingOptions.UISamplerState,
                blendState: BlendState.AlphaBlend,
                effect: DefaultEffect);

            foreach (var drawable  in drawLayers[layerOrder[i]])
            {
                drawable.OnDrawUi();
            }
            
            Core.SpriteBatch.End();
        }
    }

    public override void OnDraw()
    {
        DrawUIComponents();
    }
}