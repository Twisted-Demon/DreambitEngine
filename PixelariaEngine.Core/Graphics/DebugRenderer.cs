using System.Linq;
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
        if (!scene.DebugMode) return;
        
        Device.SetRenderTarget(null);
        
        Core.SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp, 
            blendState: BlendState.NonPremultiplied,
            sortMode: SpriteSortMode.FrontToBack,
            transformMatrix: Scene.MainCamera.TransformMatrix);

        foreach (var entity in Scene.GetAllActiveEntities())
        {
            var components = entity.GetAllActiveComponents();
            
            foreach(var component in components)
                component.OnDebugDraw();
        }
        
        Core.SpriteBatch.End();
    }
}