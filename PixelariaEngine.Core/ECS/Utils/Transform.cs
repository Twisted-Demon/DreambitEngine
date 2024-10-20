using Microsoft.Xna.Framework;

namespace PixelariaEngine.ECS;

public class Transform
{
    internal Vector3 LastWorldPosition = Vector3.Zero;

    public Vector3 Position = new(0, 0, 0);
    public Vector3 Rotation = new(0, 0, 0);
    public Vector3 Scale = new(1, 1, 1);

    internal Transform(Entity owningEntity)
    {
        Entity = owningEntity;
    }

    public Entity Entity { get; internal set; }

    public Transform Parent => Entity.Parent?.Transform;

    public Vector3 WorldPosition
    {
        get
        {
            if (Parent == null)
                return Position;

            return Vector3.Transform(Position, Parent.GetTransformationMatrix());
        }
    }

    public Vector3 WorldRotation
    {
        get
        {
            if (Parent == null)
                return Rotation;

            return Rotation + Parent.WorldRotation;
        }
    }

    public Vector3 WorldScale
    {
        get
        {
            if (Parent == null)
                return Scale;

            return Scale * Parent.WorldScale;
        }
    }

    public Vector2 WorldPosToVec2 => new(WorldPosition.X, WorldPosition.Y);
    public float WorldZRotation => WorldRotation.Z;
    public Vector2 WorldScaleToVec2 => new(WorldScale.X, WorldScale.Y);

    public Matrix GetTransformationMatrix()
    {
        var localMatrix = Matrix.CreateScale(Scale) *
                          Matrix.CreateRotationZ(Rotation.Z) *
                          Matrix.CreateRotationY(Rotation.Y) *
                          Matrix.CreateRotationX(Rotation.X) *
                          Matrix.CreateTranslation(Position);

        if (Parent == null)
            return localMatrix;

        return Parent.GetTransformationMatrix() * localMatrix;
    }
}