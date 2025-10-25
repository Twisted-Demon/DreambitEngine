using System.Collections.Generic;
using System.Linq;
using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public class ForwardRenderer2D(Scene scene) : Renderer(scene)
{
    private Effect _forwardLightingEffect;
        
    public override void Initialize()
    {
        base.Initialize();
        _forwardLightingEffect = Resources.LoadAsset<Effect>("Effects/ForwardLighting2D");
    }

    public override void OnDraw()
    {
        PreRender();
        RenderDrawables();
    }

    private void PreRender()
    {
        var drawables = Scene.Drawables.GetAllDrawables()
            .Where(x => x.Enabled && x.Entity.Enabled && x.DrawLayer != DrawLayers.LightLayer);

        foreach (var drawable in drawables)
            drawable.OnPreDraw();
    }

    private void HandleLighting()
    {
        var lights = Scene.Drawables.GetAllDrawablesByType<PointLight2D>();
        var ambientLights = Scene.Drawables.GetAllDrawablesByType<AmbientLight2D>().FirstOrDefault();
        
        var ambientColor = ambientLights != null? ambientLights.Color : Color.Black;
        
        LightingUniforms.Apply(_forwardLightingEffect, lights, Scene.MainCamera, ambientColor.ToVector3());
    }

    private void RenderDrawables()
    {
        var drawLayers = Scene.Drawables.GetDrawLayers();
        var layerOrder = drawLayers.Keys.OrderBy(x => x).ToList();
        var cameraMatrix = Scene.MainCamera.TransformMatrix;
        
        Device.SetRenderTarget(FinalRenderTarget);
        Device.Clear(Color.Transparent);

        HandleLighting();

        for (var i = 0; i < layerOrder.Count; i++)
        {
            var drawables = drawLayers[layerOrder[i]]
                .Where(x => x.Enabled && x.Entity.Enabled && x.DrawLayer != DrawLayers.LightLayer)
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
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                effect: _forwardLightingEffect
            );

            foreach (var drawable in sortedDrawables)
            {
                var drawableEffect = drawable.UsesEffect ? drawable.Effect : _forwardLightingEffect;

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