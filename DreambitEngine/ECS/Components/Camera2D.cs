using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.ECS;

public class Camera2D : Component
{
    public CameraFollowBehavior CameraFollowBehavior = CameraFollowBehavior.Lerp;
    public bool IsFollowing = true;
    public Transform TransformToFollow;

    private float ResolutionZoom { get; set; } = 1f;
    public float LerpSpeed { get; set; } = 5f;
    public float Zoom { get; set; } = 1f;

    public float PixelsPerUnit { get; set; } = 1f;

    public float TotalZoom => Zoom * ResolutionZoom;
    public float Scale => PixelsPerUnit * Zoom * ResolutionZoom;

    // Convenience: full pixels-per-world-unit including camera zoom
    private float ScreenPixelsPerWorldUnit => TotalZoom * PixelsPerUnit;
    private float WorldUnitsPerScreenPixel => 1f / ScreenPixelsPerWorldUnit;

    public int TargetVerticalResolution { get; private set; } = Window.Height;
    public Matrix TransformMatrix { get; private set; }
    public Matrix UnscaledTransformMatrix { get; private set; }

    public Matrix TopLeftTransformMatrix { get; private set; }

    public Rectangle BoundsNoZoom
    {
        get
        {
            // Get the 2D world position of the object the camera is following
            var pos = Transform.WorldPosToVec2;

            // Calculate the width and height of the camera view based on the current zoom
            var width = Window.Width / PixelsPerUnit;
            var height = Window.Height / PixelsPerUnit;

            // Return the bounds of the camera view
            return new Rectangle(
                (int)(pos.X - width * 0.5f), // Center the X position based on the zoomed width
                (int)(pos.Y - height * 0.5f), // Center the Y position based on the zoomed height
                (int)width,
                (int)height
            );
        }
    }

    public Rectangle Bounds
    {
        get
        {
            // Get the 2D world position of the object the camera is following
            var pos = Transform.WorldPosToVec2;

            // Calculate the width and height of the camera view based on the current zoom
            var width = Window.Width * WorldUnitsPerScreenPixel;
            var height = Window.Height * WorldUnitsPerScreenPixel;

            // Return the bounds of the camera view
            return new Rectangle(
                (int)(pos.X - width * 0.5f), // Center the X position based on the zoomed width
                (int)(pos.Y - height * 0.5f), // Center the Y position based on the zoomed height
                (int)width,
                (int)height
            );
        }
    }

    public RectangleF BoundsF
    {
        get
        {
            // Get the 2D world position of the object the camera is following
            var pos = Transform.WorldPosToVec2;

            // Calculate the width and height of the camera view based on the current zoom
            var width = Window.Width * WorldUnitsPerScreenPixel;
            var height = Window.Height * WorldUnitsPerScreenPixel;

            // Return the bounds of the camera view
            return new RectangleF(
                pos.X - width * 0.5f, // Center the X position based on the zoomed width
                pos.Y - height * 0.5f, // Center the Y position based on the zoomed height
                width,
                height
            );
        }
    }

    public override void OnCreated()
    {
        Window.WindowResized += OnViewportResized;
        SetResolutionZoom();
    }

    public override void OnUpdate()
    {
        UpdatePosition();
        TransformMatrix = CalculateTransformMatrix(ScreenPixelsPerWorldUnit);
        UnscaledTransformMatrix = CalculateTransformMatrix(PixelsPerUnit);
        TopLeftTransformMatrix = CalculateTopLeftMatrix(ScreenPixelsPerWorldUnit);
    }

    public override void OnDestroyed()
    {
        Window.WindowResized -= OnViewportResized;
        TransformToFollow = null;
    }

    private Matrix CalculateTransformMatrix(float scalePixelsPerUnit = 1.0f)
    {
        return Matrix.CreateTranslation(-Transform.WorldPosition) *
               Matrix.CreateRotationZ(-Transform.WorldZRotation) *
               Matrix.CreateScale(new Vector3(scalePixelsPerUnit, scalePixelsPerUnit, 1)) *
               Matrix.CreateTranslation(new Vector3(0.5f * Window.ScreenSize.X, 0.5f * Window.ScreenSize.Y, 0f));
    }

    private Matrix CalculateTopLeftMatrix(float scalePixelsPerUnit = 1.0f)
    {
        return Matrix.CreateTranslation(-Transform.WorldPosition) *
               Matrix.CreateRotationZ(-Transform.WorldZRotation) *
               Matrix.CreateScale(new Vector3(scalePixelsPerUnit, scalePixelsPerUnit, 1)) *
               Matrix.CreateTranslation(new Vector3(0.5f * Window.ScreenSize.X, 0.5f * Window.ScreenSize.Y, 0f));
    }


    private void OnViewportResized(object sender, WindowResizedEventArgs e)
    {
        SetResolutionZoom();
    }

    private void SetResolutionZoom()
    {
        var target = Math.Max(1, TargetVerticalResolution);
        ResolutionZoom = Window.Height / (float)target;
    }

    public void SetViewPort()
    {
        Core.Instance.GraphicsDevice.Viewport = new Viewport(0, 0, Window.Width, Window.Height);
    }

    public void SetTargetVerticalResolution(int targetVerticalResolution)
    {
        TargetVerticalResolution = targetVerticalResolution;
        SetResolutionZoom();
    }

    public void ForcePosition(Vector3 position)
    {
        Transform.Position = position;
    }

    private void UpdatePosition()
    {
        if (!IsFollowing || TransformToFollow == null) return;

        switch (CameraFollowBehavior)
        {
            case CameraFollowBehavior.Direct:
                DirectBehavior();
                break;
            case CameraFollowBehavior.Lerp:
                LerpBehavior();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void DirectBehavior()
    {
        Transform.Position = TransformToFollow.WorldPosition;
    }

    private void LerpBehavior()
    {
        Transform.Position = Vector3.Lerp(Transform.WorldPosition, TransformToFollow.WorldPosition,
            LerpSpeed * Time.DeltaTime);
    }

    public Vector2 WorldToScreen(Vector2 worldPos)
    {
        return Vector2.Transform(worldPos, TransformMatrix);
    }

    public Vector2 ScreenToWorld(Vector2 screenPos)
    {
        var inverse = Matrix.Invert(TransformMatrix);
        return Vector2.Transform(screenPos, inverse);
    }

    public Vector2 WorldToUiScreen(Vector2 worldPos)
    {
        return Vector2.Transform(worldPos, TopLeftTransformMatrix);
    }

    public Vector2 UIScreenToWorld(Vector2 screenPos)
    {
        var inverse = Matrix.Invert(TopLeftTransformMatrix);
        return Vector2.Transform(screenPos, inverse);
    }
}