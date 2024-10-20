using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine.Graphics;

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
        if (!scene.DebugMode) return;

        Device.SetRenderTarget(FinalRenderTarget);
        Device.Clear(Color.Transparent);

        Core.SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            blendState: BlendState.AlphaBlend,
            sortMode: SpriteSortMode.FrontToBack,
            transformMatrix: Scene.MainCamera.TransformMatrix,
            effect: DefaultEffect);

        foreach (var component in Scene.GetAllActiveEntities()
                     .Select(entity => entity.GetAllActiveComponents())
                     .SelectMany(components => components))
            component.OnDebugDraw();

        Core.SpriteBatch.End();
    }
}