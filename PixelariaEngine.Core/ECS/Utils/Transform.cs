using Microsoft.Xna.Framework;

namespace PixelariaEngine.ECS;

public class Transform
{
    public Vector3 Position = new(0, 0, 0);
    public Vector3 Rotation = new(0, 0, 0);
    public Vector3 Scale = new(1, 1, 1);
    
    public Vector2 PositionToVec2 => new Vector2(Position.X, Position.Y);
    public float SingleRotation => Rotation.Z;
    public Vector2 ScaleToVec2 => new Vector2(Scale.X, Scale.Y);

    public Transform()
    {
    }

    public Matrix GetTransformationMatrix()
    {
        return Matrix.CreateTranslation(-Position) *
            Matrix.CreateRotationX(Rotation.X) *
            Matrix.CreateRotationY(Rotation.Y) * 
            Matrix.CreateRotationZ(Rotation.Z) * 
            Matrix.CreateScale(Scale);
    }
}