using System;
using Microsoft.Xna.Framework;
using PixelariaEngine.Graphics;

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
    

    public override void OnCreated()
    {
        Window.WindowResized += OnViewportResized;
        SetResolutionZoom();
    }

    public override void OnUpdate()
    {
        TransformMatrix = CalculateTransformMatrix(ResolutionZoom * Zoom);
        UnscaledTransformMatrix = CalculateTransformMatrix();
        UpdatePosition();
    }

    public override void OnDestroyed()
    {
        Window.WindowResized -= OnViewportResized;
    }

    private Matrix CalculateTransformMatrix(float scaleFactor = 1.0f)
    {
        return Matrix.CreateTranslation(-Transform.WorldPosition) * 
               Matrix.CreateRotationZ(Transform.WorldZRotation) * 
               Matrix.CreateScale(new Vector3(scaleFactor, scaleFactor, 1)) * 
               Matrix.CreateTranslation(new Vector3(0.5f * Window.ScreenSize.X, 0.5f * Window.ScreenSize.Y, 0f));
    }
    

    private void OnViewportResized(object sender, WindowEventArgs e)
    {
        SetResolutionZoom();
    }

    private void SetResolutionZoom()
    {
        ResolutionZoom = Window.ScreenSize.Y / (float)TargetVerticalResolution;
    }

    public void SetTargetVerticalResolution(int targetVerticalResolution)
    {
        TargetVerticalResolution = targetVerticalResolution;
        SetResolutionZoom();
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
        Transform.Position = Vector3.Lerp(Transform.WorldPosition, TransformToFollow.WorldPosition, LerpSpeed * Time.DeltaTime);
    }
}