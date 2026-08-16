using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public sealed class BloomPass : RenderPass
{
    private Effect _extractEffect;
    private Effect _blurEffect;
    private Effect _compositeEffect;

    private RenderTarget2D _brightRt;
    private RenderTarget2D _blurHorizontalRt;
    private RenderTarget2D _blurVerticalRt;

    private RenderTarget2D _sceneCopyRt;

    private PostProcessSettings _settings;

    public override void Initialize()
    {
        _extractEffect =
            Resources.LoadAsset<Effect>(
                "Effects/BloomExtract");

        _blurEffect =
            Resources.LoadAsset<Effect>(
                "Effects/BloomBlur");

        _compositeEffect =
            Resources.LoadAsset<Effect>(
                "Effects/BloomComposite");

        ArgumentNullException.ThrowIfNull(
            _extractEffect);

        ArgumentNullException.ThrowIfNull(
            _blurEffect);

        ArgumentNullException.ThrowIfNull(
            _compositeEffect);

        _settings =
            Scene.PostProcessSettings;

        CreateRenderTargets();
    }

    public override void OnDraw()
    {
        if (!_settings.BloomEnabled)
            return;

        if (_settings.BloomIntensity <= 0.0f)
            return;

        ExtractBrightPixels();

        BlurHorizontal();
        BlurVertical();

        CopyScene();

        CompositeBloom();
    }

    private void ExtractBrightPixels()
    {
        _extractEffect
            .Parameters["Threshold"]
            ?.SetValue(
                MathF.Max(
                    _settings.BloomThreshold,
                    0.0f));

        _extractEffect
            .Parameters["SoftKnee"]
            ?.SetValue(
                MathHelper.Clamp(
                    _settings.BloomSoftKnee,
                    0.0f,
                    1.0f));

        Device.SetRenderTarget(
            _brightRt);

        Device.Clear(
            Color.Transparent);

        Core.SpriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            _extractEffect);

        Core.SpriteBatch.Draw(
            RenderPipeline.SceneRenderTarget,
            new Rectangle(
                0,
                0,
                _brightRt.Width,
                _brightRt.Height),
            Color.White);

        Core.SpriteBatch.End();
    }

    private void BlurHorizontal()
    {
        SetBlurParameters(
            Vector2.UnitX);

        Device.SetRenderTarget(
            _blurHorizontalRt);

        Device.Clear(
            Color.Transparent);

        Core.SpriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            _blurEffect);

        Core.SpriteBatch.Draw(
            _brightRt,
            new Rectangle(
                0,
                0,
                _blurHorizontalRt.Width,
                _blurHorizontalRt.Height),
            Color.White);

        Core.SpriteBatch.End();
    }

    private void BlurVertical()
    {
        SetBlurParameters(
            Vector2.UnitY);

        Device.SetRenderTarget(
            _blurVerticalRt);

        Device.Clear(
            Color.Transparent);

        Core.SpriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            _blurEffect);

        Core.SpriteBatch.Draw(
            _blurHorizontalRt,
            new Rectangle(
                0,
                0,
                _blurVerticalRt.Width,
                _blurVerticalRt.Height),
            Color.White);

        Core.SpriteBatch.End();
    }

    private void SetBlurParameters(
        Vector2 direction)
    {
        var texelSize =
            new Vector2(
                1.0f /
                _brightRt.Width,

                1.0f /
                _brightRt.Height);

        _blurEffect
            .Parameters["TexelSize"]
            ?.SetValue(texelSize);

        _blurEffect
            .Parameters["Direction"]
            ?.SetValue(direction);
    }

    private void CopyScene()
    {
        Device.SetRenderTarget(
            _sceneCopyRt);

        Device.Clear(
            Color.Transparent);

        Core.SpriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);

        Core.SpriteBatch.Draw(
            RenderPipeline.SceneRenderTarget,
            new Rectangle(
                0,
                0,
                _sceneCopyRt.Width,
                _sceneCopyRt.Height),
            Color.White);

        Core.SpriteBatch.End();
    }

    private void CompositeBloom()
    {
        _compositeEffect
            .Parameters["BloomTexture"]
            ?.SetValue(
                _blurVerticalRt);

        _compositeEffect
            .Parameters["BloomIntensity"]
            ?.SetValue(
                MathF.Max(
                    _settings.BloomIntensity,
                    0.0f));

        Device.SetRenderTarget(
            RenderPipeline.SceneRenderTarget);

        Device.Clear(
            Color.Transparent);

        Core.SpriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            _compositeEffect);

        Core.SpriteBatch.Draw(
            _sceneCopyRt,
            new Rectangle(
                0,
                0,
                RenderPipeline.SceneRenderTarget.Width,
                RenderPipeline.SceneRenderTarget.Height),
            Color.White);

        Core.SpriteBatch.End();
    }

    private void CreateRenderTargets()
    {
        CleanupRenderTargets();

        var halfWidth =
            Math.Max(
                1,
                ViewportSize.X / 2);

        var halfHeight =
            Math.Max(
                1,
                ViewportSize.Y / 2);

        _brightRt =
            RenderPipeline.CreateRenderTarget(
                halfWidth,
                halfHeight);

        _blurHorizontalRt =
            RenderPipeline.CreateRenderTarget(
                halfWidth,
                halfHeight);

        _blurVerticalRt =
            RenderPipeline.CreateRenderTarget(
                halfWidth,
                halfHeight);

        _sceneCopyRt =
            RenderPipeline.CreateViewportRenderTarget();
    }

    private void CleanupRenderTargets()
    {
        _brightRt?.Dispose();
        _brightRt = null;

        _blurHorizontalRt?.Dispose();
        _blurHorizontalRt = null;

        _blurVerticalRt?.Dispose();
        _blurVerticalRt = null;

        _sceneCopyRt?.Dispose();
        _sceneCopyRt = null;
    }

    protected override void OnViewportResized()
    {
        CreateRenderTargets();
    }

    protected override void OnDisposing()
    {
        CleanupRenderTargets();

        if (_extractEffect is not null)
            Resources.UnloadAsset(
                _extractEffect.Name);

        if (_blurEffect is not null)
            Resources.UnloadAsset(
                _blurEffect.Name);

        if (_compositeEffect is not null)
            Resources.UnloadAsset(
                _compositeEffect.Name);

        _extractEffect = null;
        _blurEffect = null;
        _compositeEffect = null;
    }
}