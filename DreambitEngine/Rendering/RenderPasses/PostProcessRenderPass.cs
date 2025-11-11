using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public class PostProcessRenderPass : RenderPass
{
    private Effect _colorCorrectionEffect;

    private RenderTarget2D _colorCorrectionPass;

    private PostProcessSettings _postProcessSettings;
    private Effect _tintEffect;
    private RenderTarget2D _tintPass;


    public override void Initialize()
    {
        _colorCorrectionEffect = Resources.LoadAsset<Effect>("Effects/ColorCorrection");
        _colorCorrectionPass = RenderPipeline.CreateRenderTarget();

        _tintEffect = Resources.LoadAsset<Effect>("Effects/Tint");
        _tintPass = RenderPipeline.CreateRenderTarget();

        _postProcessSettings = Scene.PostProcessSettings;
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
        _colorCorrectionEffect.Parameters["hueShift"].SetValue(_postProcessSettings.HueShift);
        _colorCorrectionEffect.Parameters["saturation"].SetValue(_postProcessSettings.Saturation);
    }

    private void ApplyTintValues()
    {
        _tintEffect.Parameters["tintColor"].SetValue(_postProcessSettings.TintColor.ToVector4());
    }

    private void Draw()
    {
        //Color correction pass
        {
            Device.SetRenderTarget(_colorCorrectionPass);
            Device.Clear(Color.Transparent);

            Core.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.PointClamp, effect: _colorCorrectionEffect);

            Core.SpriteBatch.Draw(RenderPipeline.SceneRenderTarget, Vector2.Zero, Color.White);

            Core.SpriteBatch.End();
        }

        //Tint pass
        {
            Device.SetRenderTarget(_tintPass);
            Device.Clear(Color.Transparent);

            Core.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.PointClamp, effect: _tintEffect);

            Core.SpriteBatch.Draw(_colorCorrectionPass, Vector2.Zero, Color.White);

            Core.SpriteBatch.End();
        }

        // Final Combined Pass
        {
            Device.SetRenderTarget(RenderPipeline.SceneRenderTarget); // render to the final render target
            Device.Clear(Color.Transparent);

            Core.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.PointClamp);

            Core.SpriteBatch.Draw(_tintPass, Vector2.Zero, Color.White); // color correction

            Core.SpriteBatch.End();
        }
    }

    protected override void OnWindowResized(object sender, WindowResizedEventArgs args)
    {
        base.OnWindowResized(sender, args);
        _colorCorrectionPass?.Dispose();
        _colorCorrectionPass = RenderPipeline.CreateRenderTarget();

        _tintPass?.Dispose();
        _tintPass = RenderPipeline.CreateRenderTarget();
    }

    protected override void OnDisposing()
    {
        _colorCorrectionPass?.Dispose();
        _colorCorrectionPass = null;

        _tintPass?.Dispose();
        _tintPass = null;

        Resources.UnloadAsset(_tintEffect.Name);
        Resources.UnloadAsset(_colorCorrectionEffect.Name);
    }
}