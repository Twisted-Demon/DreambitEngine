using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine.Graphics;

public class DefaultRenderer(Scene scene) : Renderer(scene)
{
    private RenderTarget2D _renderBuffer;
    
    public override void Initialize()
    {
        _renderBuffer = new RenderTarget2D(
            Device,
            Device.PresentationParameters.BackBufferWidth,
            Device.PresentationParameters.BackBufferHeight
        );
    }

    public override void OnDraw()
    {
        RenderDrawables();
        ShowRenderBuffer();
    }

    private void ShowRenderBuffer()
    { 
        Device.SetRenderTarget(null);

        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        Core.SpriteBatch.Draw(_renderBuffer, Vector2.Zero, Color.White);
        
        Core.SpriteBatch.End();
    }

    private void RenderDrawables()
    {
        //Device.SetRenderTarget(_renderBuffer);
        Device.Clear(Scene.BackgroundColor);
        
        Core.SpriteBatch.Begin(transformMatrix: scene.MainCamera.TransformMatrix, samplerState: SamplerState.PointClamp);
        //loop through the drawable components
        var drawables = Scene.Drawables.GetAllDrawables()
            .Where(x => x.Enabled && x.Entity.Enabled && x.DrawLayer != RenderLayers.LightLayer);

        foreach (var drawable in drawables)
        {
            drawable.OnDraw();
        }
        
        Core.SpriteBatch.End();
    }
    
    public override void CleanUp()
    {
        _renderBuffer.Dispose();
    }
}