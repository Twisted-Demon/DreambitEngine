using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.Graphics;

public class DebugRenderPass : RenderPass
{
    public override void Initialize()
    {
        base.Initialize();
        Order = 1;
    }

    public override void OnDraw()
    {
        IsActive = Scene.DebugMode;

        if (!Scene.DebugMode) return;

        Device.SetRenderTarget(RenderPipeline.SceneRenderTarget);

        Core.SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            blendState: BlendState.AlphaBlend,
            sortMode: SpriteSortMode.Deferred,
            transformMatrix: Scene.MainCamera.TransformMatrix,
            effect: DefaultEffect);

        var entities = Scene.GetAllActiveEntities(); // ideally IReadOnlyList<Entity>
        foreach (var entity in entities)
        {
            var comps = entity.GetAllActiveComponents();
            foreach (var c in comps) c.OnDebugDraw();
        }

        Core.SpriteBatch.End();
    }
}