using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine;

public class DefaultRenderer(Scene scene) : Renderer(scene)
{
    private List<RenderTarget2D> _layerRenderTargets = [];

    public override void OnDraw()
    {
        UpdateRenderTargets();
        PreRender();
        RenderDrawables();
        RenderToFinalRenderTarget();
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
        if (_layerRenderTargets.Count == Scene.Drawables.DrawLayerCount) return;

        foreach (var renderTarget in _layerRenderTargets)
            renderTarget.Dispose();

        _layerRenderTargets.Clear();

        for (var i = 0; i < Scene.Drawables.DrawLayerCount; i++) _layerRenderTargets.Add(CreateRenderTarget());
    }

    private void RenderToFinalRenderTarget()
    {
        Device.SetRenderTarget(FinalRenderTarget);
        Device.Clear(Color.Transparent);

        //fogEffect.Parameters["fogStart"].SetValue(25f);
        //fogEffect.Parameters["fogEnd"].SetValue(1000f);
        //fogEffect.Parameters["fogColor"].SetValue(new Vector4(0.75f, 0.75f, 0.75f, 1f));
        //
        //fogEffect.Parameters["noiseScale"].SetValue(new Vector2(1f, 1f));
        //fogEffect.Parameters["noiseOffset"].SetValue(Vector2.Zero);
        //
        //fogEffect.Parameters["NoiseSampler"].SetValue(_fogNoiseTexture);


        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.NonPremultiplied,
            sortMode: SpriteSortMode.Immediate, effect: DefaultEffect);

        foreach (var renderTarget in _layerRenderTargets)
            Core.SpriteBatch.Draw(renderTarget, Vector2.Zero, Color.White);

        Core.SpriteBatch.End();
    }

    private void RenderDrawables()
    {
        var drawLayers = Scene.Drawables.GetDrawLayers();
        var layerOrder = drawLayers.Keys.OrderBy(x => x).ToList();
        var cameraMatrix = Scene.MainCamera.TransformMatrix;

        for (var i = 0; i < layerOrder.Count; i++)
        {
            Device.SetRenderTarget(_layerRenderTargets[i]);
            Device.Clear(Color.Transparent);

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

    protected override void OnWindowResized(object sender, WindowEventArgs args)
    {
        base.OnWindowResized(sender, args);

        var renderTargetCount = _layerRenderTargets.Count;

        foreach (var renderTarget in _layerRenderTargets) renderTarget.Dispose();

        _layerRenderTargets.Clear();

        for (var i = 0; i < renderTargetCount; i++) _layerRenderTargets.Add(CreateRenderTarget());
    }

    protected override void OnCleanUp()
    {
        foreach (var renderTarget in _layerRenderTargets)
            renderTarget?.Dispose();

        _layerRenderTargets.Clear();
        _layerRenderTargets = null;
    }
}