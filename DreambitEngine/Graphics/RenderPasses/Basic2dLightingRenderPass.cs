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
        var layerOrder = drawLayers.Keys.OrderBy(x => x).ToList();
        var cameraMatrix = Scene.MainCamera.TransformMatrix;

        Device.SetRenderTarget(AlbedoRt);
        Device.Clear(Color.Transparent);

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
                samplerState: Scene.RenderingOptions.SamplerState,
                sortMode: SpriteSortMode.Deferred,
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
                        samplerState: Scene.RenderingOptions.SamplerState,
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
            samplerState: SamplerState.PointClamp,
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            effect: LightingFx
        );

        Core.SpriteBatch.Draw(AlbedoRt, Vector2.Zero, Color.White);

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