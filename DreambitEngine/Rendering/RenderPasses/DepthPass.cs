using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public class DepthPass : RenderPass
{
    private SortDrawablesPass SortDrawablesPass { get; set; }
    private AlbedoPass AlbedoPass { get; set; }
    private RenderTarget2D AlbedoRt => AlbedoPass.AlbedoRt;

    private DreambitEffect DepthFx { get; set; }
    public RenderTarget2D DepthRt { get; private set; }

    public override void Initialize()
    {
        SortDrawablesPass = RenderPipeline.GetRenderPass<SortDrawablesPass>();
        ArgumentNullException.ThrowIfNull(SortDrawablesPass);

        AlbedoPass = RenderPipeline.GetRenderPass<AlbedoPass>();
        ArgumentNullException.ThrowIfNull(AlbedoPass);

        DepthFx = Resources.LoadAsset<DreambitEffect>("Effects/Depth2D");
        ArgumentNullException.ThrowIfNull(DepthFx);
        ArgumentNullException.ThrowIfNull(DepthFx.Effect);
    }

    public override void OnDraw()
    {
        Device.SetRenderTarget(DepthRt);
        Device.Clear(Color.Transparent);

        RenderSceneDepth();
    }

    private void RenderSceneDepth()
    {
        var sceneRenderList = SortDrawablesPass.SceneRenderList;

        for (var i = 0; i < sceneRenderList.Count; i++)
        {
            var entry = sceneRenderList[i];
            
            DepthFx.Effect.Parameters["SortDepth"]
                ?.SetValue(entry.SortDepth);


            Core.SpriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Opaque,
                Scene.RenderingOptions.SamplerState,
                DepthStencilState.None,
                RasterizerState.CullNone,
                DepthFx,
                RenderCamera.TransformMatrix);

            entry.Drawable.Draw();

            Core.SpriteBatch.End();
        }
    }

    protected override void OnViewportResized()
    {
        CreateDepthRenderTarget();
    }

    protected override void OnDisposing()
    {
        CleanupDepthRenderTarget();
        Resources.UnloadAsset(DepthFx.AssetName);
    }

    private void CreateDepthRenderTarget()
    {
        DepthRt?.Dispose();
        ArgumentNullException.ThrowIfNull(AlbedoRt);
        DepthRt = new RenderTarget2D(
            Device,
            AlbedoRt.Width,
            AlbedoRt.Height,
            false,
            SurfaceFormat.Single,
            DepthFormat.None);
    }

    private void CleanupDepthRenderTarget()
    {
        DepthRt?.Dispose();
        DepthRt = null;
    }
}