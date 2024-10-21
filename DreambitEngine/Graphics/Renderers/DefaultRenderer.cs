using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public class DefaultRenderer(Scene scene) : Renderer(scene)
{

    public override void OnDraw()
    {
        PreRender();
        RenderDrawables();
    }

    private void PreRender()
    {
        var drawables = Scene.Drawables.GetAllDrawables()
            .Where(x => x.Enabled && x.Entity.Enabled && x.DrawLayer != RenderLayers.LightLayer);

        foreach (var drawable in drawables)
            drawable.OnPreDraw();
    }

    private void RenderDrawables()
    {
        var drawLayers = Scene.Drawables.GetDrawLayers();
        var layerOrder = drawLayers.Keys.OrderBy(x => x).ToList();
        var cameraMatrix = Scene.MainCamera.TransformMatrix;
        
        Device.SetRenderTarget(FinalRenderTarget);
        Device.Clear(Color.Transparent);

        for (var i = 0; i < layerOrder.Count; i++)
        {
            var drawables = drawLayers[layerOrder[i]]
                .Where(x => x.Enabled && x.Entity.Enabled && x.DrawLayer != RenderLayers.LightLayer)
                .ToList();

            var visibleDrawables = drawables
                .Where(d => d.IsVisibleFromCamera(Scene.MainCamera.Bounds))
                .ToList();

            if (visibleDrawables.Count == 0) continue;

            var sortedDrawables = visibleDrawables
                .OrderBy(d => d.Transform.WorldPosition.Y)
                .ThenBy(d => d.UsesEffect ? d.Effect : DefaultEffect)
                .ToList();

            Effect currentEffect = null;

            Core.SpriteBatch.Begin(
                transformMatrix: cameraMatrix,
                samplerState: SamplerState.PointClamp,
                sortMode: SpriteSortMode.FrontToBack,
                blendState: BlendState.AlphaBlend,
                effect: DefaultEffect
            );

            foreach (var drawable in sortedDrawables)
            {
                var drawableEffect = drawable.UsesEffect ? drawable.Effect : DefaultEffect;

                if (drawableEffect != currentEffect)
                {
                    Core.SpriteBatch.End();
                    Core.SpriteBatch.Begin(
                        transformMatrix: cameraMatrix,
                        samplerState: SamplerState.PointClamp,
                        sortMode: SpriteSortMode.Deferred,
                        blendState: BlendState.AlphaBlend,
                        effect: drawableEffect
                    );
                    currentEffect = drawableEffect;
                }

                drawable.OnDraw();
            }

            Core.SpriteBatch.End();
        }
    }
    
}