using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

//public class TransformOld
//{
//    internal Vector3 LastWorldPosition = Vector3.Zero;
//
//    public Vector3 Position = new(0, 0, 0);
//    public Vector3 Rotation = new(0, 0, 0);
//    public Vector3 Scale = new(1, 1, 1);
//
//    internal Transform(Entity owningEntity)
//    {
//        Entity = owningEntity;
//    }
//
//    public Entity Entity { get; internal set; }
//
//    public Transform Parent => Entity.Parent?.Transform;
//
//    public Vector3 WorldPosition
//    {
//        get
//        {
//            if (Parent == null)
//                return Position;
//
//            return Vector3.Transform(
//                Position, 
//                Parent.GetTransformationMatrix());
//        }
//    }
//
//    public Vector3 WorldRotation =>
//        Parent == null
//            ? Rotation
//            : Rotation + Parent.WorldRotation;
//
//    public Vector3 WorldScale =>
//        Parent == null
//            ? Scale
//            : Scale * Parent.WorldScale;
//
//    public Vector2 WorldPosToVec2 =>
//        new(WorldPosition.X, WorldPosition.Y);
//
//    public Vector2 WorldScaleToVec2 =>
//        new(WorldScale.X, WorldScale.Y);
//
//    public float WorldZRotation =>
//        WorldRotation.Z;
//
//    public float WorldAngle
//    {
//        get => WorldRotation.Z;
//        set => Rotation.Z = Parent == null
//                ? value
//                : value - Parent.WorldRotation.Z;
//    }
//    
//    public Vector3 Forward
//    {
//        get
//        {
//            var rotationMatrix =
//                Matrix.CreateRotationZ(WorldRotation.Z) *
//                Matrix.CreateRotationY(WorldRotation.Y) *
//                Matrix.CreateRotationX(WorldRotation.X);
//
//            return Vector3.Normalize(
//                Vector3.TransformNormal(Vector3.UnitX, rotationMatrix));
//        }
//    }
//
//    public Vector2 Forward2D =>
//        new(
//            Mathf.Cos(WorldAngle),
//            Mathf.Sin(WorldAngle));
//
//    public Matrix GetTransformationMatrix()
//    {
//        var localMatrix = Matrix.CreateScale(Scale) *
//                          Matrix.CreateRotationZ(Rotation.Z) *
//                          Matrix.CreateRotationY(Rotation.Y) *
//                          Matrix.CreateRotationX(Rotation.X) *
//                          Matrix.CreateTranslation(Position);
//
//        if (Parent == null)
//            return localMatrix;
//
//        return localMatrix * Parent.GetTransformationMatrix();
//    }
//
//    internal void DebugDraw()
//    {
//        Core.SpriteBatch.DrawPoint(
//            WorldPosToVec2, 
//            Color.Red, 
//            3f * Scene.Instance.MainCamera.WorldUnitsPerTexturePixel);
//        
//        Core.SpriteBatch.DrawLine(
//            WorldPosToVec2, 
//            WorldPosToVec2 + (ForwardVec2 * 0.5f), 
//            Color.Red,
//            2f * Scene.Instance.MainCamera.WorldUnitsPerTexturePixel);
//    }
//}