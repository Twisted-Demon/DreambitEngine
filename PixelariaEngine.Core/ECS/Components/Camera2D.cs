using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine.ECS;

public class Camera2D : Component
{
    public CameraFollowBehavior CameraFollowBehavior = CameraFollowBehavior.Lerp;
    public bool IsFollowing = true;
    public Transform TransformToFollow;
    private float ResolutionZoom { get; set; } = 1f;
    public float LerpSpeed { get; set; } = 5f;
    public float Zoom { get; set; } = 1f;
    public float TotalZoom => Zoom * ResolutionZoom;
    public int TargetVerticalResolution { get; private set; } = 288;
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
            var width = Window.Width;
            var height = Window.Height;

            // Return the bounds of the camera view
            return new Rectangle(
                (int)(pos.X - width * 0.5f), // Center the X position based on the zoomed width
                (int)(pos.Y - height * 0.5f), // Center the Y position based on the zoomed height
                width,
                height
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
            var width = (int)(Window.Width / TotalZoom);
            var height = (int)(Window.Height / TotalZoom);

            // Return the bounds of the camera view
            return new Rectangle(
                (int)(pos.X - width * 0.5f), // Center the X position based on the zoomed width
                (int)(pos.Y - height * 0.5f), // Center the Y position based on the zoomed height
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
        TransformMatrix = CalculateTransformMatrix(ResolutionZoom * Zoom);
        UnscaledTransformMatrix = CalculateTransformMatrix();
        TopLeftTransformMatrix = CalculateTopLeftMatrix(TotalZoom);
        UpdatePosition();
    }

    public override void OnDestroyed()
    {
        Window.WindowResized -= OnViewportResized;
        TransformToFollow = null;
    }

    private Matrix CalculateTransformMatrix(float scaleFactor = 1.0f)
    {
        return Matrix.CreateTranslation(-Transform.WorldPosition) *
               Matrix.CreateScale(new Vector3(scaleFactor, scaleFactor, 1)) *
               Matrix.CreateRotationZ(Transform.WorldZRotation) *
               Matrix.CreateTranslation(new Vector3(0.5f * Window.ScreenSize.X, 0.5f * Window.ScreenSize.Y, 0f));
    }

    private Matrix CalculateTopLeftMatrix(float scaleFactor = 1.0f)
    {
        return Matrix.CreateTranslation(-Transform.WorldPosition) *
               Matrix.CreateScale(new Vector3(scaleFactor, scaleFactor, 1)) *
               Matrix.CreateRotationZ(Transform.WorldZRotation) *
               Matrix.CreateTranslation(new Vector3(0.5f * Window.ScreenSize.X, 0.5f * Window.ScreenSize.Y, 0f));
    }


    private void OnViewportResized(object sender, WindowEventArgs e)
    {
        SetResolutionZoom();
    }

    private void SetResolutionZoom()
    {
        ResolutionZoom = Window.Height / (float)TargetVerticalResolution;
    }

    public void SetViewPort()
    {
        var topLeft = new Vector2(Transform.WorldPosition.X - Window.ScreenSize.X / (float)2,
            Transform.WorldPosition.Y - Window.ScreenSize.Y / (float)2);

        Core.Instance.GraphicsDevice.Viewport = new Viewport(
            (int)topLeft.X, (int)topLeft.Y, Window.Width, Window.Height
        );
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
}