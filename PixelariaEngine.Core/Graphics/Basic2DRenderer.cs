using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine.Graphics;

public class Basic2DRenderer(Scene scene) : Renderer(scene)
{
    private SpriteBatch _spriteBatch;
    private RenderTarget2D _renderBuffer;
    
    public override void Initialize()
    {
        _spriteBatch = new SpriteBatch(Core.Instance.GraphicsDevice);
        
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

        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
        
        _spriteBatch.Draw(_renderBuffer, Vector2.Zero, Color.White);
        
        _spriteBatch.End();
    }

    private void RenderDrawables()
    {
        Device.SetRenderTarget(_renderBuffer);
        Device.Clear(Scene.BackgroundColor);
        
        _spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, transformMatrix: scene.Camera2D.TransformMatrix, samplerState: SamplerState.PointClamp); // begin sprite batch
        
        //loop through the drawable components
        var drawables = Scene.Drawables.GetAllDrawables()
            .Where(x => x.Enabled && x.Entity.Enabled && x.DrawLayer != RenderLayers.LightLayer);

        foreach (var drawable in drawables)
        {
            drawable.OnDraw(_spriteBatch);
        }
        
        _spriteBatch.End();
    }
    
    public override void CleanUp()
    {
        _spriteBatch.Dispose();
        _renderBuffer.Dispose();
    }
}