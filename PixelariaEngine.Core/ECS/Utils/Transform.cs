using Microsoft.Xna.Framework;

namespace PixelariaEngine.ECS;

public class Transform
{
    public Entity Entity { get; internal set; }
    
    public Vector3 Position = new(0, 0, 0);
    public Vector3 Rotation = new(0, 0, 0);
    public Vector3 Scale = new(1, 1, 1);

    public Transform Parent => Entity.Parent?.Transform;

    internal Transform(Entity owningEntity)
    {
        Entity = owningEntity;
    }

    public Vector3 WorldPosition
    {
        get
        {
            if(Parent == null)
                return Position;

            return Vector3.Transform(Position, Parent.GetTransformationMatrix());
        }
    }

    public Vector3 WorldRotation
    {
        get
        {
            if (Parent == null)
                return Position;
            
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
        var localMatrix = Matrix.CreateTranslation(Position) *
               Matrix.CreateRotationX(Rotation.X) *
               Matrix.CreateRotationY(Rotation.Y) *
               Matrix.CreateRotationZ(Rotation.Z) *
               Matrix.CreateScale(Scale);
        
        if(Parent == null)
            return localMatrix;
        
        return localMatrix * Parent.GetTransformationMatrix();
    }
}