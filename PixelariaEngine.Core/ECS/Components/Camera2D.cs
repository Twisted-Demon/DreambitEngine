using System;
using Microsoft.Xna.Framework;
using PixelariaEngine.Graphics;

namespace PixelariaEngine.ECS;

public class Camera2D : Component
{
    public CameraFollowBehavior CameraFollowBehavior = CameraFollowBehavior.Lerp;
    public bool IsFollowing = true;
    public Transform TransformToFollow;
    public float LerpSpeed { get; set; } = 0.1f;
    public float Zoom { get; set; } = 1f;
    public float ResolutionZoom { get; set; } = 1f;
    private int TargetHorizontalResolution { get; } = 384;
    public Matrix TransformMatrix { get; private set; }
    public Matrix UnscaledTransformMatrix { get; private set; }

    private static Point ScreenSize
        => new(Core.Instance.GraphicsDevice.Viewport.Width, Core.Instance.GraphicsDevice.Viewport.Height);


    public override void OnCreated()
    {
        Window.WindowResized += OnViewportResized;
        SetResolutionZoom();
    }

    public override void OnUpdate()
    {
        TransformMatrix = Transform.GetTransformationMatrix() *
                          Matrix.CreateScale(new Vector3(ResolutionZoom * Zoom, ResolutionZoom * Zoom, 1)) *
                          Matrix.CreateTranslation(new Vector3(0.5f * ScreenSize.X, 0.5f * ScreenSize.Y, 0f));
        
        UnscaledTransformMatrix = Transform.GetTransformationMatrix() * 
                                  //Matrix.CreateScale(new Vector3(Zoom, Zoom, 1)) * 
                                  Matrix.CreateTranslation(new Vector3(0.5f * ScreenSize.X, 0.5f * ScreenSize.Y, 0f));

        UpdatePosition();
    }

    public override void OnDestroyed()
    {
        Window.WindowResized -= OnViewportResized;
    }

    private void OnViewportResized(object sender, WindowEventArgs e)
    {
        SetResolutionZoom();
    }

    private void SetResolutionZoom()
    {
        ResolutionZoom = ScreenSize.X / (float)TargetHorizontalResolution;
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
        Transform.Position = TransformToFollow.Position;
    }

    private void LerpBehavior()
    {
        Transform.Position = Vector3.Lerp(Transform.Position, TransformToFollow.Position, LerpSpeed);
    }
}