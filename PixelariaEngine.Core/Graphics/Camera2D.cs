using System;
using System.Data.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Graphics;

public class Camera2D
{
    public Vector2 Position = new(0, 0);
    public Transform TransformToFollow;
    public bool IsFollowing = true;
    public CameraFollowBehavior CameraFollowBehavior = CameraFollowBehavior.Direct;
    public float Zoom { get; set; } = 2.8125f;
    public float Rotation { get; set; } = 0f;
    public float LerpSpeed { get; set; } = 0.1f;
    
    public Matrix TransformMatrix { get; private set; }

    public Point ScreenSize 
        => new(Core.Instance.GraphicsDevice.Viewport.Width, Core.Instance.GraphicsDevice.Viewport.Height);

    internal void Update()
    {
        TransformMatrix = Matrix.CreateTranslation(new Vector3(-Position, 0)) *
                     Matrix.CreateRotationZ(Rotation) *
                     Matrix.CreateScale(new Vector3(Zoom, Zoom, 1)) * 
                     Matrix.CreateTranslation(new Vector3(0.5f * ScreenSize.X, 0.5f * ScreenSize.Y, 0f));
        
        UpdatePosition();
        
    }

    private void UpdatePosition()
    {
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
        Position = TransformToFollow.Position;
    }

    private void LerpBehavior()
    {
        var start = Position;
        var end = TransformToFollow.Position;

        var result = start + (end - start) * 0.1f;
        
        Position = result;
    }
}