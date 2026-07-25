using System.Linq;
using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public class Basic2dLightingRenderPass : RenderPass
{
    private Effect LightingFx { get; set; }

    private RenderTarget2D AlbedoRt { get; set; }

    public override void Initialize()
    {
        base.Initialize();
        CreateAlbedoRenderTarget();
        LightingFx = Resources.LoadAsset<Effect>("Effects/ForwardLighting2D");
    }

    public override void OnDraw()
    {
        RenderDrawables();
        RenderLighting();
    }

    private void RenderDrawables()
    {
        var drawLayers = Drawables.GetDrawLayers();

        var layerOrder = drawLayers.Keys
            .OrderBy(layer => layer)
            .ToList();

        var cameraMatrix = Scene.MainCamera.TransformMatrix;

        Device.SetRenderTarget(AlbedoRt);
        Device.Clear(Color.Transparent);

        foreach (var layer in layerOrder)
        {
            var visibleDrawables = drawLayers[layer]
                .Where(drawable =>
                    drawable.Enabled &&
                    drawable.Entity.Enabled &&
                    drawable.DrawLayer != DrawLayers.LightLayer &&
                    drawable.IsVisibleFromCamera(Scene.MainCamera.Bounds))
                .OrderBy(
                    drawable => drawable,
                    new DrawableComparer(DefaultEffect))
                .ToList();
                
                
            if (visibleDrawables.Count == 0)
                continue;

            Effect currentEffect = null;
            var batchStarted = false;

            foreach (var drawable in visibleDrawables)
            {
                var drawableEffect = drawable.UsesEffect
                    ? drawable.Effect
                    : DefaultEffect;

                if (!batchStarted ||
                    !ReferenceEquals(drawableEffect, currentEffect))
                {
                    if (batchStarted) Core.SpriteBatch.End();

                    Core.SpriteBatch.Begin(
                        SpriteSortMode.Deferred,
                        Scene.RenderingOptions.BlendState,
                        Scene.RenderingOptions.SamplerState,
                        DepthStencilState.None,
                        RasterizerState.CullNone,
                        drawableEffect,
                        cameraMatrix
                    );

                    currentEffect = drawableEffect;
                    batchStarted = true;
                }

                drawable.OnDraw();
            }

            if (batchStarted) Core.SpriteBatch.End();
        }
    }

    private void RenderLighting()
    {
        var lights = Drawables.GetAllDrawablesByType<PointLight2D>()
            .Where(x => x.IsVisibleFromCamera(Scene.MainCamera.Bounds)).ToList();

        var ambientLight = Drawables.GetAllDrawablesByType<AmbientLight2D>().FirstOrDefault();

        var ambientColor = ambientLight != null ? ambientLight.Color : Color.Black;

        LightingUniforms.Apply(LightingFx, lights, Scene.MainCamera, ambientColor.ToVector3());

        Device.SetRenderTarget(RenderPipeline.SceneRenderTarget);
        Device.Clear(Color.Transparent);

        Core.SpriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            Scene.RenderingOptions.SamplerState,
            DepthStencilState.None,
            RasterizerState.CullNone,
            LightingFx,
            Matrix.Identity
        );

        Core.SpriteBatch.Draw(
            AlbedoRt,
            new Rectangle(
                0,
                0,
                RenderPipeline.SceneRenderTarget.Width,
                RenderPipeline.SceneRenderTarget.Height
            ),
            Color.White
        );

        Core.SpriteBatch.End();
    }

    protected override void OnWindowResized(object sender, WindowResizedEventArgs args)
    {
        base.OnWindowResized(sender, args);

        CreateAlbedoRenderTarget();
    }

    protected override void OnDisposing()
    {
        base.OnDisposing();
        CleanupAlbedoRenderTarget();
        Resources.UnloadAsset(LightingFx.Name);
    }

    private void CreateAlbedoRenderTarget()
    {
        AlbedoRt?.Dispose();
        AlbedoRt = RenderPipeline.CreateRenderTarget();
    }

    private void CleanupAlbedoRenderTarget()
    {
        AlbedoRt?.Dispose();
        AlbedoRt = null;
    }
}