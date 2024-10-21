
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public class PostProcessRenderer : Renderer
{
    private readonly Renderer _sceneRenderer;
    private Effect _colorCorrectionEffect;
    private Effect _tintEffect;

    private RenderTarget2D _colorCorrectionPass;
    private RenderTarget2D _tintPass;
    
    public PostProcessRenderer(Scene scene, Renderer sceneRenderer) : base(scene)
    {
        _sceneRenderer = sceneRenderer;
    }

    public override void Initialize()
    {
        _colorCorrectionEffect = Resources.LoadAsset<Effect>("Effects/ColorCorrection");
        _colorCorrectionPass = CreateRenderTarget();
        
        _tintEffect = Resources.LoadAsset<Effect>("Effects/Tint");
        _tintPass = CreateRenderTarget();
    }

    public override void OnDraw()
    {
        PreDraw();
        Draw();
    }

    private void PreDraw()
    {
        ApplyColorCorrectionValues();
        ApplyTintValues();
    }

    private void ApplyColorCorrectionValues()
    {
        _colorCorrectionEffect.Parameters["hueShift"].SetValue(0.0f);
        _colorCorrectionEffect.Parameters["saturation"].SetValue(.75f);
    }

    private void ApplyTintValues()
    {
        _tintEffect.Parameters["tintColor"].SetValue(new Color(180, 180, 220, 255).ToVector4());
    }
    
    private void Draw()
    {
        if (_sceneRenderer.FinalRenderTarget == null)
            return;
        
        //Color correction pass
        {
            Device.SetRenderTarget(_colorCorrectionPass);
            Device.Clear(Color.Transparent);
        
            Core.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, 
                samplerState: SamplerState.PointClamp, effect: _colorCorrectionEffect);
        
            Core.SpriteBatch.Draw(_sceneRenderer.FinalRenderTarget, Vector2.Zero, Color.White);
        
            Core.SpriteBatch.End();
        }
        
        //Tint pass
        {
            Device.SetRenderTarget(_tintPass);
            Device.Clear(Color.Transparent);
            
            Core.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, 
                samplerState: SamplerState.PointClamp, effect: _tintEffect);
        
            Core.SpriteBatch.Draw(_colorCorrectionPass, Vector2.Zero, Color.White);
        
            Core.SpriteBatch.End();
        }

        // Final Combined Pass
        {
            Device.SetRenderTarget(FinalRenderTarget); // render to the final render target
            Device.Clear(Color.Transparent);
            
            Core.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, 
                samplerState: SamplerState.PointClamp);
        
            Core.SpriteBatch.Draw(_tintPass, Vector2.Zero, Color.White); // color correction
        
            Core.SpriteBatch.End();
        }
    }

    protected override void OnWindowResized(object sender, WindowEventArgs args)
    {
        base.OnWindowResized(sender, args);
        _colorCorrectionPass?.Dispose();
        _colorCorrectionPass = CreateRenderTarget();
        
        _tintPass?.Dispose();
        _tintPass = CreateRenderTarget();
    }

    protected override void OnCleanUp()
    {
        _colorCorrectionPass?.Dispose();
        _colorCorrectionPass = null;
    }
}