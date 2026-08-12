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
        EnsureTarget(width, height);
        var camera = scene.EnsureEditorCamera();
        camera.Transform.Position = new Vector3(position, camera.Transform.Position.Z);
        camera.Zoom = EditorViewportUi.NormalizeZoom(zoom);
        camera.ConfigureEditorViewport(_sceneTarget!.Width, _sceneTarget.Height);

        _device.SetRenderTarget(_sceneTarget);
        _device.Clear(scene.BackgroundColor);
        try
        {
            BuildDrawBuffer(scene);
            Draw(scene, camera.TransformMatrix);
            Present(scene.RenderingOptions.SamplerState);
            LastError = null;
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
        }
        finally
        {
            _device.SetRenderTarget(null);
        }
        return camera;
    }

    public Entity? Pick(Scene scene, Vector2 worldPosition)
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
            catch
            {
                // A custom bounds implementation is an extension boundary; keep picking others.
            }
        }

        Entity? nearest = null;
        var nearestDistanceSquared = 100f;
        foreach (var entity in scene.GetAllEntities())
        {
            if (entity.IsEditorOnly && !entity.IsLDtkGenerated)
                continue;
            var distanceSquared = Vector2.DistanceSquared(entity.Transform.WorldPosition2D, worldPosition);
            if (distanceSquared >= nearestDistanceSquared)
                continue;
            nearest = entity;
            nearestDistanceSquared = distanceSquared;
        }
        return nearest;
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
        drawable.Enabled && drawable.Entity.Enabled;

    private void Draw(Scene scene, Matrix transform)
    {
        Effect? activeEffect = null;
        var batchStarted = false;
        foreach (var drawable in _drawBuffer)
        {
            var effect = drawable.Effect;
            if (!batchStarted || !ReferenceEquals(activeEffect, effect))
            {
                if (batchStarted)
                    Core.SpriteBatch.End();
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
        if (batchStarted)
            Core.SpriteBatch.End();
    }

    private void Present(SamplerState samplerState)
    {
        var presentEffect = Resources.LoadAsset<Effect>("Effects/Present")
                            ?? throw new InvalidOperationException(
                                "The built-in scene presentation effect could not be loaded.");
        _device.SetRenderTarget(_displayTarget);
        _device.Clear(Color.Transparent);
        Core.SpriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.Opaque,
            samplerState,
            DepthStencilState.None,
            RasterizerState.CullNone,
            presentEffect);
        Core.SpriteBatch.Draw(_sceneTarget!, Vector2.Zero, Color.White);
        Core.SpriteBatch.End();
    }

    private void EnsureTarget(int width, int height)
    {
        width = Math.Clamp(width, 1, 8192);
        height = Math.Clamp(height, 1, 8192);
        if (_sceneTarget is not null && _sceneTarget.Width == width && _sceneTarget.Height == height)
            return;

        if (_textureId != 0)
            _imGui.UnbindTexture(_textureId);
        _sceneTarget?.Dispose();
        _displayTarget?.Dispose();
        _sceneTarget = new RenderTarget2D(
            _device,
            width,
            height,
            false,
            RenderPipeline.SceneColorFormat,
            DepthFormat.None)
        {
            Name = "Dreambit Editor Linear Scene View"
        };
        _displayTarget = new RenderTarget2D(
            _device,
            width,
            height,
            false,
            SurfaceFormat.Color,
            DepthFormat.None)
        {
            Name = "Dreambit Editor Display Scene View"
        };
        _textureId = _imGui.BindTexture(_displayTarget);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        if (_textureId != 0)
            _imGui.UnbindTexture(_textureId);
        _sceneTarget?.Dispose();
        _displayTarget?.Dispose();
        _sceneTarget = null;
        _displayTarget = null;
        _textureId = 0;
        _disposed = true;
    }
}
