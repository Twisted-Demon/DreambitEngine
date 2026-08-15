using Dreambit.ECS;
using Dreambit.Editor.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.Editor.Graphics;

internal sealed class SceneViewportRenderer : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly ImGuiRenderer _imGui;
    private readonly List<DrawableComponent> _drawBuffer = new(512);
    private RenderTarget2D? _sceneTarget;
    private RenderTarget2D? _lightingTarget;
    private RenderTarget2D? _colorCorrectionTarget;
    private RenderTarget2D? _displayTarget;
    private nint _textureId;
    private bool _disposed;

    public SceneViewportRenderer(GraphicsDevice device, ImGuiRenderer imGui)
    {
        _device = device;
        _imGui = imGui;
    }

    public RenderTarget2D? Target => _displayTarget;
    public nint TextureId => _textureId;
    public string? LastError { get; private set; }

    public Camera2D Render(Scene scene, int width, int height, Vector2 position, float zoom)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Camera2D? camera = null;

        try
        {
            camera = scene.EnsureEditorCamera();
            EnsureTarget(width, height);
            camera.Transform.Position = new Vector3(position, camera.Transform.Position.Z);
            camera.Zoom = EditorViewportUi.NormalizeZoom(zoom);
            camera.ConfigureEditorViewport(_sceneTarget!.Width, _sceneTarget.Height);
            _device.SetRenderTarget(_sceneTarget);
            _device.Clear(Color.Transparent);
            BuildDrawBuffer(scene);
            Draw(scene, camera.TransformMatrix);
            ApplyLighting(scene, camera, scene.RenderingOptions.SamplerState);
            ApplyColorCorrection(scene, scene.RenderingOptions.SamplerState);
            Present(scene.RenderingOptions.SamplerState, scene.PostProcessSettings.TintColor);
            LastError = null;
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
        }
        finally
        {
            _drawBuffer.Clear();
            try
            {
                _device.SetRenderTarget(null);
            }
            catch (Exception exception)
            {
                LastError ??= exception.Message;
            }
        }
        // EnsureEditorCamera is the only operation above that can leave this unset. It
        // is an engine invariant for an editor scene, so preserve that failure rather
        // than returning an invalid camera to interaction code.
        if (camera is null)
            throw new InvalidOperationException(
                $"The editor camera for '{scene.GetType().FullName}' could not be created. {LastError}");
        return camera;
    }

    public Entity? Pick(Scene scene, Vector2 worldPosition)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        try
        {
            BuildDrawBuffer(scene);
            for (var index = _drawBuffer.Count - 1; index >= 0; index--)
            {
                var drawable = _drawBuffer[index];
                try
                {
                    if (drawable.Bounds.Contains(worldPosition))
                        return drawable.Entity;
                }
                catch (Exception exception)
                {
                    // A custom bounds implementation is an extension boundary. Keep
                    // picking other drawables, but surface the fault in the viewport.
                    LastError ??=
                        $"{drawable.GetType().FullName ?? drawable.GetType().Name} " +
                        $"could not provide picking bounds: {exception.Message}";
                }
            }

            Entity? nearest = null;
            var nearestDistanceSquared = 100f;
            foreach (var entity in scene.GetAllEntities())
            {
                if (entity.IsEditorOnly && !entity.IsImportedMapGenerated)
                    continue;
                var distanceSquared = Vector2.DistanceSquared(entity.Transform.WorldPosition2D, worldPosition);
                if (distanceSquared >= nearestDistanceSquared)
                    continue;
                nearest = entity;
                nearestDistanceSquared = distanceSquared;
            }
            return nearest;
        }
        finally
        {
            _drawBuffer.Clear();
        }
    }

    private void BuildDrawBuffer(Scene scene)
    {
        _drawBuffer.Clear();
        foreach (var drawable in scene.GetAllDrawables())
            if (ShouldRenderDrawable(drawable))
                _drawBuffer.Add(drawable);
        _drawBuffer.Sort(static (left, right) =>
        {
            var layer = left.DrawLayer.CompareTo(right.DrawLayer);
            return layer != 0 ? layer : left.SortDepth.CompareTo(right.SortDepth);
        });
    }

    // Editor-only means transient/non-serialized, not invisible. This keeps linked
    // LDtk level geometry visible while selection and hierarchy editing remain disabled.
    internal static bool ShouldRenderDrawable(DrawableComponent drawable) =>
        drawable is not Light2D && drawable.Enabled && drawable.Entity.Enabled;

    private void Draw(Scene scene, Matrix transform)
    {
        Effect? activeEffect = null;
        var batchStarted = false;
        try
        {
            foreach (var drawable in _drawBuffer)
            {
                var effect = drawable.Effect;
                if (!batchStarted || !ReferenceEquals(activeEffect, effect))
                {
                    EndBatch(ref batchStarted);
                    Core.SpriteBatch.Begin(
                        SpriteSortMode.Deferred,
                        scene.RenderingOptions.BlendState,
                        scene.RenderingOptions.SamplerState,
                        DepthStencilState.None,
                        RasterizerState.CullNone,
                        effect,
                        transform);
                    activeEffect = effect;
                    batchStarted = true;
                }
                drawable.Draw();
            }
        }
        finally
        {
            EndBatch(ref batchStarted);
        }
    }

    private void Present(SamplerState samplerState, Color tintColor)
    {
        var presentEffect = Resources.LoadAsset<Effect>("Effects/Present")
                            ?? throw new InvalidOperationException(
                                "The built-in scene presentation effect could not be loaded.");
        _device.SetRenderTarget(_displayTarget);
        _device.Clear(Color.Transparent);
        var batchStarted = false;
        try
        {
            Core.SpriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Opaque,
                samplerState,
                DepthStencilState.None,
                RasterizerState.CullNone,
                presentEffect);
            batchStarted = true;
            // Applying the tint as the SpriteBatch color before the Present shader is
            // equivalent to the runtime Tint pass, while avoiding a fourth HDR target.
            Core.SpriteBatch.Draw(_colorCorrectionTarget!, Vector2.Zero, tintColor);
        }
        finally
        {
            EndBatch(ref batchStarted);
        }
    }

    private void ApplyLighting(Scene scene, Camera2D camera, SamplerState samplerState)
    {
        var lightingEffect = Resources.LoadAsset<Effect>("Effects/ForwardLighting2D")
                             ?? throw new InvalidOperationException(
                                 "The built-in 2D lighting effect could not be loaded.");
        var pointLights = scene.GetAllDrawables()
            .OfType<PointLight2D>()
            .Where(light => light.Enabled && light.Entity.Enabled && light.IsVisibleFromCamera(camera.BoundsF))
            .ToArray();
        var ambient = scene.Settings.AmbientLightColor.ToVector3() *
                      scene.Settings.AmbientLightIntensity;
        LightingUniforms.Apply(lightingEffect, pointLights, camera, ambient);

        _device.SetRenderTarget(_lightingTarget);
        _device.Clear(Color.Transparent);
        Core.SpriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            samplerState,
            DepthStencilState.None,
            RasterizerState.CullNone,
            lightingEffect,
            Matrix.Identity);
        try
        {
            Core.SpriteBatch.Draw(
                _sceneTarget!,
                new Rectangle(0, 0, _lightingTarget!.Width, _lightingTarget.Height),
                Color.White);
        }
        finally
        {
            Core.SpriteBatch.End();
        }
    }

    private void ApplyColorCorrection(Scene scene, SamplerState samplerState)
    {
        var colorCorrectionEffect = Resources.LoadAsset<Effect>("Effects/ColorCorrection")
                                    ?? throw new InvalidOperationException(
                                        "The built-in color-correction effect could not be loaded.");
        colorCorrectionEffect.Parameters["hueShift"]?.SetValue(scene.PostProcessSettings.HueShift);
        colorCorrectionEffect.Parameters["saturation"]?.SetValue(scene.PostProcessSettings.Saturation);

        _device.SetRenderTarget(_colorCorrectionTarget);
        _device.Clear(Color.Transparent);
        Core.SpriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.AlphaBlend,
            samplerState,
            DepthStencilState.None,
            RasterizerState.CullNone,
            colorCorrectionEffect);
        try
        {
            Core.SpriteBatch.Draw(_lightingTarget!, Vector2.Zero, Color.White);
        }
        finally
        {
            Core.SpriteBatch.End();
        }
    }

    private static void EndBatch(ref bool batchStarted)
    {
        if (!batchStarted)
            return;
        batchStarted = false;
        Core.SpriteBatch.End();
    }

    private void EnsureTarget(int width, int height)
    {
        width = Math.Clamp(width, 1, 8192);
        height = Math.Clamp(height, 1, 8192);
        if (_sceneTarget is not null &&
            _lightingTarget is not null &&
            _colorCorrectionTarget is not null &&
            _displayTarget is not null &&
            _textureId != 0 &&
            _sceneTarget.Width == width &&
            _sceneTarget.Height == height &&
            _lightingTarget.Width == width &&
            _lightingTarget.Height == height &&
            _colorCorrectionTarget.Width == width &&
            _colorCorrectionTarget.Height == height &&
            _displayTarget.Width == width &&
            _displayTarget.Height == height)
        {
            return;
        }

        RenderTarget2D? replacementScene = null;
        RenderTarget2D? replacementLighting = null;
        RenderTarget2D? replacementColorCorrection = null;
        RenderTarget2D? replacementDisplay = null;
        nint replacementTextureId = 0;
        try
        {
            replacementScene = new RenderTarget2D(
                _device,
                width,
                height,
                false,
                RenderPipeline.SceneColorFormat,
                DepthFormat.None)
            {
                Name = "Dreambit Editor Albedo Scene View"
            };
            replacementLighting = new RenderTarget2D(
                _device,
                width,
                height,
                false,
                RenderPipeline.SceneColorFormat,
                DepthFormat.None)
            {
                Name = "Dreambit Editor Lit Scene View"
            };
            replacementColorCorrection = new RenderTarget2D(
                _device,
                width,
                height,
                false,
                RenderPipeline.SceneColorFormat,
                DepthFormat.None)
            {
                Name = "Dreambit Editor Color Corrected Scene View"
            };
            replacementDisplay = new RenderTarget2D(
                _device,
                width,
                height,
                false,
                SurfaceFormat.Color,
                DepthFormat.None)
            {
                Name = "Dreambit Editor Display Scene View"
            };
            replacementTextureId = _imGui.BindTexture(replacementDisplay);
        }
        catch (Exception allocationFailure)
        {
            List<Exception>? cleanupFailures = null;
            TryCleanup(
                () =>
                {
                    if (replacementTextureId != 0)
                        _imGui.UnbindTexture(replacementTextureId);
                },
                ref cleanupFailures);
            TryCleanup(() => replacementScene?.Dispose(), ref cleanupFailures);
            TryCleanup(() => replacementLighting?.Dispose(), ref cleanupFailures);
            TryCleanup(() => replacementColorCorrection?.Dispose(), ref cleanupFailures);
            TryCleanup(() => replacementDisplay?.Dispose(), ref cleanupFailures);
            if (cleanupFailures is not null)
            {
                cleanupFailures.Insert(0, allocationFailure);
                throw new AggregateException(
                    "Could not allocate or clean up replacement viewport targets.",
                    cleanupFailures);
            }
            throw;
        }

        // Publish only a complete target pair and binding. If allocation failed, the
        // previous renderer state above remains usable on the next frame.
        var previousScene = _sceneTarget;
        var previousLighting = _lightingTarget;
        var previousColorCorrection = _colorCorrectionTarget;
        var previousDisplay = _displayTarget;
        var previousTextureId = _textureId;
        _sceneTarget = replacementScene;
        _lightingTarget = replacementLighting;
        _colorCorrectionTarget = replacementColorCorrection;
        _displayTarget = replacementDisplay;
        _textureId = replacementTextureId;

        List<Exception>? replacementCleanupFailures = null;
        TryCleanup(
            () =>
            {
                if (previousTextureId != 0)
                    _imGui.UnbindTexture(previousTextureId);
            },
            ref replacementCleanupFailures);
        TryCleanup(() => previousScene?.Dispose(), ref replacementCleanupFailures);
        TryCleanup(() => previousLighting?.Dispose(), ref replacementCleanupFailures);
        TryCleanup(() => previousColorCorrection?.Dispose(), ref replacementCleanupFailures);
        TryCleanup(() => previousDisplay?.Dispose(), ref replacementCleanupFailures);
        if (replacementCleanupFailures is not null)
        {
            throw new AggregateException(
                "The new viewport targets were installed, but the previous targets could not be fully released.",
                replacementCleanupFailures);
        }
    }

    private static void TryCleanup(Action cleanup, ref List<Exception>? failures)
    {
        try
        {
            cleanup();
        }
        catch (Exception exception)
        {
            (failures ??= []).Add(exception);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        var sceneTarget = _sceneTarget;
        var lightingTarget = _lightingTarget;
        var colorCorrectionTarget = _colorCorrectionTarget;
        var displayTarget = _displayTarget;
        var textureId = _textureId;
        _drawBuffer.Clear();
        _sceneTarget = null;
        _lightingTarget = null;
        _colorCorrectionTarget = null;
        _displayTarget = null;
        _textureId = 0;
        _disposed = true;

        List<Exception>? failures = null;
        TryCleanup(
            () =>
            {
                if (textureId != 0)
                    _imGui.UnbindTexture(textureId);
            },
            ref failures);
        TryCleanup(() => sceneTarget?.Dispose(), ref failures);
        TryCleanup(() => lightingTarget?.Dispose(), ref failures);
        TryCleanup(() => colorCorrectionTarget?.Dispose(), ref failures);
        TryCleanup(() => displayTarget?.Dispose(), ref failures);
        if (failures is not null)
            throw new AggregateException("Could not fully dispose the scene viewport renderer.", failures);
    }
}
