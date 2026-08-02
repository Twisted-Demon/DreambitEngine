using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dreambit.ECS;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class UIRenderPass : RenderPass
{
    private RasterizerState _scissorRasterizerState;

    public override void Initialize()
    {
        base.Initialize();
        Order = 2;
        _scissorRasterizerState = new RasterizerState
        {
            CullMode = CullMode.None,
            ScissorTestEnable = true
        };
    }

    private void DrawUIComponents()
    {
        var drawLayers = Scene.Drawables.GetDrawLayers();
        var layerOrder = drawLayers.Keys.OrderBy(x => x).ToList();

        Device.SetRenderTarget(RenderPipeline.SceneRenderTarget);
        
        for (var i = 0; i < layerOrder.Count; i++)
        {
            Core.SpriteBatch.Begin(
                transformMatrix: Scene.UiCamera.TopLeftTransformMatrix,
                sortMode: SpriteSortMode.Immediate,
                samplerState: Scene.RenderingOptions.UISamplerState,
                blendState: BlendState.AlphaBlend,
                rasterizerState: _scissorRasterizerState,
                effect: DefaultEffect);

            foreach (var drawable  in drawLayers[layerOrder[i]])
            {
                if (!drawable.Enabled || !drawable.Entity.Enabled)
                    continue;

                drawable.OnDrawUi();
            }
            
            Core.SpriteBatch.End();
        }
    }

    public override void OnDraw()
    {
        DrawUIComponents();
    }

    protected override void OnDisposing()
    {
        _scissorRasterizerState?.Dispose();
        _scissorRasterizerState = null;
        base.OnDisposing();
    }
}
