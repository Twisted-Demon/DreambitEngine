using Dreambit.ECS;
using Dreambit.Editor.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.Editor.Graphics;

internal sealed class SceneViewportRenderer : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly ImGuiRenderer _imGui;
    private readonly Action<string, Exception?>? _reportError;
    private readonly List<DrawableComponent> _pickBuffer = new(512);
    private RenderTarget2D? _displayTarget;
    private nint _textureId;
    private bool _disposed;
    private string? _lastReportedError;

    public SceneViewportRenderer(
        GraphicsDevice device,
        ImGuiRenderer imGui,
        Action<string, Exception?>? reportError = null)
    {
        _device = device;
        _imGui = imGui;
        _reportError = reportError;
    }

    public RenderTarget2D? Target => _displayTarget;
    public nint TextureId => _textureId;
    public string? LastError { get; private set; }

    public Camera2D Render(Scene scene, int width, int height, Vector2 position, float zoom)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Camera2D? camera = null;
        LastError = null;

        try
        {
            camera = scene.EnsureEditorCamera();
            EnsureTarget(width, height);
            camera.Transform.Position = new Vector3(position, camera.Transform.Position.Z);
            camera.Zoom = EditorViewportUi.NormalizeZoom(zoom);
            camera.ConfigureEditorViewport(_displayTarget!.Width, _displayTarget.Height);
            scene.RenderTo(_displayTarget, camera);
            LastError = null;
        }
        catch (Exception exception)
        {
            CaptureError("Could not render the scene viewport.", exception);
        }
        finally
        {
            try
            {
                _device.SetRenderTarget(null);
            }
            catch (Exception exception)
            {
                CaptureError("Could not restore the graphics backbuffer after rendering the scene viewport.", exception);
            }
        }
        if (LastError is null)
            _lastReportedError = null;

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
            BuildPickBuffer(scene);
            Entity? importedMapHit = null;
            for (var index = _pickBuffer.Count - 1; index >= 0; index--)
            {
                var drawable = _pickBuffer[index];
                try
                {
                    if (!drawable.Bounds.Contains(worldPosition))
                        continue;

                    // Imported tile and map-background bounds commonly cover the
                    // complete playable area. Keep that hit as a fallback, but let
                    // authored scene content win wherever the two overlap.
                    if (ShouldDeferImportedMapPick(drawable))
                    {
                        importedMapHit ??= drawable.Entity;
                        continue;
                    }

                    return drawable.Entity;
                }
                catch (Exception exception)
                {
                    // A custom bounds implementation is an extension boundary. Keep
                    // picking other drawables, but surface the fault in the viewport.
                    CaptureError(
                        $"{drawable.GetType().FullName ?? drawable.GetType().Name} " +
                        "could not provide picking bounds.",
                        exception);
                }
            }

            if (importedMapHit is not null)
                return importedMapHit;

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
            _pickBuffer.Clear();
        }
    }

    private void BuildPickBuffer(Scene scene)
    {
        _pickBuffer.Clear();
        foreach (var drawable in scene.GetAllDrawables())
            if (ShouldPickDrawable(drawable))
                _pickBuffer.Add(drawable);
        _pickBuffer.Sort(static (left, right) =>
        {
            var layer = left.DrawLayer.CompareTo(right.DrawLayer);
            return layer != 0 ? layer : left.SortDepth.CompareTo(right.SortDepth);
        });
    }

    // Editor-only means transient/non-serialized, not unpickable. This keeps linked
    // map geometry selectable while hierarchy editing remains disabled.
    internal static bool ShouldPickDrawable(DrawableComponent drawable) =>
        drawable is not Light2D && drawable.Enabled && drawable.Entity.Enabled;

    internal static bool ShouldDeferImportedMapPick(DrawableComponent drawable) =>
        drawable.Entity.IsImportedMapGenerated;

    private void EnsureTarget(int width, int height)
    {
        width = Math.Clamp(width, 1, 8192);
        height = Math.Clamp(height, 1, 8192);
        if (_displayTarget is not null &&
            _textureId != 0 &&
            _displayTarget.Width == width &&
            _displayTarget.Height == height)
        {
            return;
        }

        RenderTarget2D? replacementDisplay = null;
        nint replacementTextureId = 0;
        try
        {
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
            TryCleanup(() => replacementDisplay?.Dispose(), ref cleanupFailures);
            if (cleanupFailures is not null)
            {
                cleanupFailures.Insert(0, allocationFailure);
                throw new AggregateException(
                    "Could not allocate or clean up the replacement viewport target.",
                    cleanupFailures);
            }
            throw;
        }

        // Publish only a complete target and binding. If allocation failed, the
        // previous renderer state remains usable on the next frame.
        var previousDisplay = _displayTarget;
        var previousTextureId = _textureId;
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
        TryCleanup(() => previousDisplay?.Dispose(), ref replacementCleanupFailures);
        if (replacementCleanupFailures is not null)
        {
            throw new AggregateException(
                "The new viewport target was installed, but the previous target could not be fully released.",
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

    private void CaptureError(string message, Exception exception)
    {
        LastError ??= $"{message} {exception.Message}";
        var signature = message + Environment.NewLine + exception;
        if (string.Equals(_lastReportedError, signature, StringComparison.Ordinal))
            return;

        _lastReportedError = signature;
        try
        {
            _reportError?.Invoke(message, exception);
        }
        catch (Exception reportingFailure)
        {
            Console.Error.WriteLine(
                $"{message} {exception}{Environment.NewLine}" +
                $"Reporting the editor error also failed: {reportingFailure}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        var displayTarget = _displayTarget;
        var textureId = _textureId;
        _pickBuffer.Clear();
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
        TryCleanup(() => displayTarget?.Dispose(), ref failures);
        if (failures is not null)
            throw new AggregateException("Could not fully dispose the scene viewport renderer.", failures);
    }
}
