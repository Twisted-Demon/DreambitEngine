using System;
using System.Linq;
using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public class BasicLightingPass : RenderPass
{
    private DreambitEffect LightingFx { get; set; }

    private AlbedoPass AlbedoPass { get; set; }
    private RenderTarget2D AlbedoRt => AlbedoPass.AlbedoRt;

    public override void Initialize()
    {
        AlbedoPass = RenderPipeline.GetRenderPass<AlbedoPass>();
        ArgumentNullException.ThrowIfNull(AlbedoPass);
        AlbedoPass.RendersToSceneRt = false; // need this so we can get the albedoRT
        // instead of rendering to scene immediately

        LightingFx = Resources.LoadAsset<DreambitEffect>("Effects/BasicLighting2D");
        ArgumentNullException.ThrowIfNull(LightingFx);
        ArgumentNullException.ThrowIfNull(LightingFx.Effect);
    }

    public override void OnDraw()
    {
        RenderLighting();
    }

    private void RenderLighting()
    {
        var lights = Drawables.GetAllDrawablesByType<PointLight2D>()
            .Where(x => x.IsVisibleFromCamera(RenderCamera.BoundsF)).ToList();

        var ambientLight = Drawables.GetAllDrawablesByType<AmbientLight2D>().FirstOrDefault();
        var ambientColor = ambientLight is not null
            ? ambientLight.Color.ToVector3() * ambientLight.Intensity
            : Scene.Settings.AmbientLightColor.ToVector3() *
              Scene.Settings.AmbientLightIntensity;

        LightingUniforms.Apply(LightingFx, lights, RenderCamera, ambientColor);

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


    protected override void OnDisposing()
    {
        base.OnDisposing();
        LightingFx = null;
    }
}
