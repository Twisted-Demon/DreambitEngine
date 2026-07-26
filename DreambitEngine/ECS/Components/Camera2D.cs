using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.ECS;

public class Camera2D : Component
{
    private const float MinimumScale = 0.0001f;

    public CameraFollowBehavior CameraFollowBehavior =
        CameraFollowBehavior.Lerp;

    public bool IsFollowing = true;
    public Transform TransformToFollow;

    private float _resolutionZoom = 1f;
    private float _zoom = 1f;
    private float _pixelsPerUnit = 1f;

    private bool _matricesDirty = true;

    private Vector3 _lastCameraPosition;
    private float _lastCameraRotation;
    private Vector2 _lastViewportSize;

    private Matrix _inverseTransformMatrix;
    private Matrix _inverseUnscaledTransformMatrix;
    private Matrix _inverseTopLeftTransformMatrix;

    public float LerpSpeed { get; set; } = 5f;

    /// <summary>
    /// Camera magnification.
    /// Must be greater than zero.
    /// </summary>
    public float Zoom
    {
        get => _zoom;
        set
        {
            ValidatePositiveFinite(value, nameof(Zoom));

            if (_zoom == value)
                return;

            _zoom = value;
            _matricesDirty = true;
        }
    }

    /// <summary>
    /// Number of physical screen pixels used by one world unit before zoom.
    /// For example, 16 means one world unit occupies 16 pixels at Zoom 1.
    /// </summary>
    public float PixelsPerUnit
    {
        get => _pixelsPerUnit;
        set
        {
            ValidatePositiveFinite(value, nameof(PixelsPerUnit));

            if (_pixelsPerUnit == value)
                return;

            _pixelsPerUnit = value;
            _matricesDirty = true;
        }
    }

    private float ResolutionZoom
    {
        get => _resolutionZoom;
        set
        {
            ValidatePositiveFinite(value, nameof(ResolutionZoom));

            if (_resolutionZoom == value)
                return;

            _resolutionZoom = value;
            _matricesDirty = true;
        }
    }

    /// <summary>
    /// Camera zoom after resolution scaling.
    /// Does not include PixelsPerUnit.
    /// </summary>
    public float TotalZoom => Zoom * ResolutionZoom;

    /// <summary>
    /// Final number of screen pixels occupied by one world unit.
    /// </summary>
    public float Scale => PixelsPerUnit * TotalZoom;

    private float ScreenPixelsPerWorldUnit => Scale;

    private float NoCameraZoomPixelsPerWorldUnit =>
        PixelsPerUnit * ResolutionZoom;

    private float WorldUnitsPerScreenPixel =>
        1f / ScreenPixelsPerWorldUnit;

    private Vector2 ViewportSize =>
        new(Window.Width, Window.Height);

    public int TargetVerticalResolution { get; private set; } =
        Math.Max(1, Window.Height);

    /// <summary>
    /// Normal camera matrix. The camera position appears at screen center.
    /// Includes PixelsPerUnit, Zoom, and ResolutionZoom.
    /// </summary>
    public Matrix TransformMatrix { get; private set; } = Matrix.Identity;

    /// <summary>
    /// Camera matrix without the user-controlled Zoom value.
    /// PixelsPerUnit and ResolutionZoom are still applied.
    /// </summary>
    public Matrix UnscaledTransformMatrix { get; private set; } =
        Matrix.Identity;

    /// <summary>
    /// Camera-relative matrix where the camera position maps to pixel 0,0.
    /// This is not the normal world-to-screen matrix.
    /// </summary>
    public Matrix TopLeftTransformMatrix { get; private set; } =
        Matrix.Identity;

    public Rectangle Bounds =>
        ToEnclosingRectangle(BoundsF);

    public Rectangle BoundsNoZoom
    {
        get
        {
            EnsureMatricesCurrent();

            return ToEnclosingRectangle(
                CalculateWorldBounds(_inverseUnscaledTransformMatrix));
        }
    }

    /// <summary>
    /// Width and height of the visible area in world units.
    /// This does not include the camera's world position.
    ///
    /// For a rotated camera, use BoundsF when you need an axis-aligned
    /// world-space culling rectangle.
    /// </summary>
    public Rectangle BoundsNoPosition
    {
        get
        {
            EnsureMatricesCurrent();

            var viewport = ViewportSize;

            var width = viewport.X * WorldUnitsPerScreenPixel;
            var height = viewport.Y * WorldUnitsPerScreenPixel;

            return new Rectangle(
                0,
                0,
                (int)MathF.Ceiling(width),
                (int)MathF.Ceiling(height));
        }
    }

    /// <summary>
    /// Axis-aligned world-space bounds enclosing the visible viewport.
    /// Correctly accounts for camera rotation.
    /// </summary>
    public RectangleF BoundsF
    {
        get
        {
            EnsureMatricesCurrent();
            return CalculateWorldBounds(_inverseTransformMatrix);
        }
    }

    public override void OnCreated()
    {
        Window.WindowResized += OnViewportResized;

        SetResolutionZoom();

        // Ensure conversions are valid before the first OnUpdate call.
        EnsureMatricesCurrent();
    }

    public override void OnUpdate()
    {
        UpdatePosition();

        // This also detects camera transform and viewport changes.
        EnsureMatricesCurrent();
    }

    public override void OnDestroyed()
    {
        Window.WindowResized -= OnViewportResized;
        TransformToFollow = null;
    }

    private static void ValidatePositiveFinite(
        float value,
        string parameterName)
    {
        if (!float.IsFinite(value) || value < MinimumScale)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Value must be finite and at least {MinimumScale}.");
        }
    }

    /// <summary>
    /// Rebuilds matrices when any relevant camera state has changed.
    ///
    /// This means conversion methods remain correct even when PixelsPerUnit,
    /// Zoom, position, rotation, or viewport size changes between updates.
    /// </summary>
    private void EnsureMatricesCurrent()
    {
        var cameraPosition = Transform.WorldPosition;
        var cameraRotation = Transform.WorldZRotation;
        var viewportSize = ViewportSize;

        var transformChanged =
            cameraPosition != _lastCameraPosition ||
            cameraRotation != _lastCameraRotation;

        var viewportChanged =
            viewportSize != _lastViewportSize;

        if (!_matricesDirty &&
            !transformChanged &&
            !viewportChanged)
        {
            return;
        }

        RebuildMatrices(
            cameraPosition,
            cameraRotation,
            viewportSize);
    }

    private void RebuildMatrices(
        Vector3 cameraPosition,
        float cameraRotation,
        Vector2 viewportSize)
    {
        TransformMatrix = CalculateCenteredTransformMatrix(
            cameraPosition,
            cameraRotation,
            viewportSize,
            ScreenPixelsPerWorldUnit);

        _inverseTransformMatrix =
            Matrix.Invert(TransformMatrix);

        // "Unscaled" means no user-controlled camera Zoom.
        // PixelsPerUnit and resolution scaling must still be included.
        UnscaledTransformMatrix = CalculateCenteredTransformMatrix(
            cameraPosition,
            cameraRotation,
            viewportSize,
            NoCameraZoomPixelsPerWorldUnit);

        _inverseUnscaledTransformMatrix =
            Matrix.Invert(UnscaledTransformMatrix);

        // Camera-relative coordinates. Camera position maps to 0,0.
        TopLeftTransformMatrix = CalculateCameraRelativeTransformMatrix(
            cameraPosition,
            cameraRotation,
            ScreenPixelsPerWorldUnit);

        _inverseTopLeftTransformMatrix =
            Matrix.Invert(TopLeftTransformMatrix);

        _lastCameraPosition = cameraPosition;
        _lastCameraRotation = cameraRotation;
        _lastViewportSize = viewportSize;
        _matricesDirty = false;
    }

    private static Matrix CalculateCenteredTransformMatrix(
        Vector3 cameraPosition,
        float cameraRotation,
        Vector2 viewportSize,
        float pixelsPerWorldUnit)
    {
        return
            Matrix.CreateTranslation(-cameraPosition) *
            Matrix.CreateRotationZ(-cameraRotation) *
            Matrix.CreateScale(
                pixelsPerWorldUnit,
                pixelsPerWorldUnit,
                1f) *
            Matrix.CreateTranslation(
                viewportSize.X * 0.5f,
                viewportSize.Y * 0.5f,
                0f);
    }

    private static Matrix CalculateCameraRelativeTransformMatrix(
        Vector3 cameraPosition,
        float cameraRotation,
        float pixelsPerWorldUnit)
    {
        return
            Matrix.CreateTranslation(-cameraPosition) *
            Matrix.CreateRotationZ(-cameraRotation) *
            Matrix.CreateScale(
                pixelsPerWorldUnit,
                pixelsPerWorldUnit,
                1f);
    }

    private RectangleF CalculateWorldBounds(Matrix inverseCameraMatrix)
    {
        var viewport = ViewportSize;

        var topLeft = Vector2.Transform(
            Vector2.Zero,
            inverseCameraMatrix);

        var topRight = Vector2.Transform(
            new Vector2(viewport.X, 0f),
            inverseCameraMatrix);

        var bottomLeft = Vector2.Transform(
            new Vector2(0f, viewport.Y),
            inverseCameraMatrix);

        var bottomRight = Vector2.Transform(
            viewport,
            inverseCameraMatrix);

        var minX = MathF.Min(
            MathF.Min(topLeft.X, topRight.X),
            MathF.Min(bottomLeft.X, bottomRight.X));

        var minY = MathF.Min(
            MathF.Min(topLeft.Y, topRight.Y),
            MathF.Min(bottomLeft.Y, bottomRight.Y));

        var maxX = MathF.Max(
            MathF.Max(topLeft.X, topRight.X),
            MathF.Max(bottomLeft.X, bottomRight.X));

        var maxY = MathF.Max(
            MathF.Max(topLeft.Y, topRight.Y),
            MathF.Max(bottomLeft.Y, bottomRight.Y));

        return new RectangleF(
            minX,
            minY,
            maxX - minX,
            maxY - minY);
    }

    private static Rectangle ToEnclosingRectangle(RectangleF bounds)
    {
        // Floor the minimums and ceil the maximums so that the integer
        // rectangle completely encloses the floating-point bounds.
        //
        // Direct casts truncate toward zero and are wrong for negative
        // coordinates.
        var left = (int)MathF.Floor(bounds.X);
        var top = (int)MathF.Floor(bounds.Y);

        var right = (int)MathF.Ceiling(
            bounds.X + bounds.Width);

        var bottom = (int)MathF.Ceiling(
            bounds.Y + bounds.Height);

        return new Rectangle(
            left,
            top,
            right - left,
            bottom - top);
    }

    private void OnViewportResized(
        object sender,
        WindowResizedEventArgs e)
    {
        SetResolutionZoom();
        _matricesDirty = true;
    }

    private void SetResolutionZoom()
    {
        var targetHeight =
            Math.Max(1, TargetVerticalResolution);

        var actualHeight =
            Math.Max(1, Window.Height);

        ResolutionZoom =
            actualHeight / (float)targetHeight;
    }

    public void SetViewPort()
    {
        Core.Instance.GraphicsDevice.Viewport =
            new Viewport(
                0,
                0,
                Window.Width,
                Window.Height);

        _matricesDirty = true;
    }

    public void SetTargetVerticalResolution(
        int targetVerticalResolution)
    {
        if (targetVerticalResolution <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetVerticalResolution),
                targetVerticalResolution,
                "Target vertical resolution must be greater than zero.");
        }

        TargetVerticalResolution =
            targetVerticalResolution;

        SetResolutionZoom();
    }

    public void ForcePosition(Vector3 position)
    {
        Transform.Position = position;
        _matricesDirty = true;

        // Note: if IsFollowing is true, UpdatePosition will replace this
        // position during the next update.
    }

    private void UpdatePosition()
    {
        if (!IsFollowing || TransformToFollow == null)
            return;

        switch (CameraFollowBehavior)
        {
            case CameraFollowBehavior.Direct:
                DirectBehavior();
                break;

            case CameraFollowBehavior.Lerp:
                LerpBehavior();
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(CameraFollowBehavior),
                    CameraFollowBehavior,
                    "Unsupported camera follow behavior.");
        }

        _matricesDirty = true;
    }

    private void DirectBehavior()
    {
        SetCameraWorldPosition(
            TransformToFollow.WorldPosition);
    }

    private void LerpBehavior()
    {
        var deltaTime =
            MathF.Max(0f, Time.DeltaTime);

        var speed =
            MathF.Max(0f, LerpSpeed);

        // Frame-rate-independent smoothing.
        // Unlike speed * deltaTime, this never exceeds 1 or overshoots.
        var interpolation =
            1f - MathF.Exp(-speed * deltaTime);

        var position = Vector3.Lerp(
            Transform.WorldPosition,
            TransformToFollow.WorldPosition,
            interpolation);

        SetCameraWorldPosition(position);
    }

    private void SetCameraWorldPosition(Vector3 worldPosition)
    {
        /*
         * This assumes the camera Transform is not parented and Position
         * therefore represents world position.
         *
         * If your Transform supports parenting, use its world-position
         * setter here instead. Assigning another object's WorldPosition
         * directly to a local Position is incorrect for a parented camera.
         */
        Transform.Position = worldPosition;
        _matricesDirty = true;
    }

    public Vector2 WorldToScreen(Vector2 worldPosition)
    {
        EnsureMatricesCurrent();

        return Vector2.Transform(
            worldPosition,
            TransformMatrix);
    }

    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        EnsureMatricesCurrent();

        return Vector2.Transform(
            screenPosition,
            _inverseTransformMatrix);
    }

    /// <summary>
    /// Converts a world position to actual UI/screen pixel coordinates.
    ///
    /// This must use the centered camera matrix. The old implementation
    /// incorrectly used TopLeftTransformMatrix.
    /// </summary>
    public Vector2 WorldToUiScreen(Vector2 worldPosition)
    {
        return WorldToScreen(worldPosition);
    }

    /// <summary>
    /// Converts actual UI/screen pixel coordinates back into world space.
    /// </summary>
    public Vector2 UIScreenToWorld(Vector2 screenPosition)
    {
        return ScreenToWorld(screenPosition);
    }

    /// <summary>
    /// Converts world coordinates into camera-relative pixel coordinates,
    /// where the camera's position is pixel 0,0.
    ///
    /// This preserves the useful behavior of the old top-left methods,
    /// but gives it an accurate name.
    /// </summary>
    public Vector2 WorldToCameraLocal(Vector2 worldPosition)
    {
        EnsureMatricesCurrent();

        return Vector2.Transform(
            worldPosition,
            TopLeftTransformMatrix);
    }

    public Vector2 CameraLocalToWorld(Vector2 cameraLocalPosition)
    {
        EnsureMatricesCurrent();

        return Vector2.Transform(
            cameraLocalPosition,
            _inverseTopLeftTransformMatrix);
    }
}