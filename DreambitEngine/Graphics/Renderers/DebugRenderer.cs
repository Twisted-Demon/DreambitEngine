using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.Graphics;

public class DebugRenderer(Scene scene) : Renderer(scene)
{
    public override void Initialize()
    {
        base.Initialize();
        Order = 1;
    }

    public override void OnDraw()
    {
        IsActive = scene.DebugMode;
        
        Device.SetRenderTarget(FinalRenderTarget);
        Device.Clear(Color.Transparent);

        if (!scene.DebugMode)
        {
            Device.SetRenderTarget(null);
            return;
        }

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
            foreach (var c in comps)
            {
                c.OnDebugDraw();
            }
        }
        
        //foreach (var component in Scene.GetAllActiveEntities()
        //             .Select(entity => entity.GetAllActiveComponents())
        //             .SelectMany(components => components))
        //    component.OnDebugDraw();

        Core.SpriteBatch.End();
    }
}