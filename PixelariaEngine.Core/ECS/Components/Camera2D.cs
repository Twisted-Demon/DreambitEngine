using System;
using System.Data.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelariaEngine.ECS;
using PixelariaEngine.Graphics;

namespace PixelariaEngine.ECS;

public class Camera2D : Component
{
    public Transform TransformToFollow;
    public bool IsFollowing = true;
    public CameraFollowBehavior CameraFollowBehavior = CameraFollowBehavior.Direct;
    public float LerpSpeed { get; set; } = 0.1f;
    public float Zoom { get; set; } = 1f;
    public Matrix TransformMatrix { get; private set; }
    private static Point ScreenSize 
        => new(Core.Instance.GraphicsDevice.Viewport.Width, Core.Instance.GraphicsDevice.Viewport.Height);

    private int TargetHorizontalResolution { get; set; } = 256;
    private float ResolutionZoom { get; set; } = 1f;
    
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
        ResolutionZoom = ScreenSize.Y / (float) TargetHorizontalResolution;
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