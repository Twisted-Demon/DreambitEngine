using System;
using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public class Transform
{
    private const float Epsilon = 0.000001f;

    internal Vector3 LastWorldPosition = Vector3.Zero;

    private Quaternion _rotation = Quaternion.Identity;

    internal Transform(Entity owningEntity)
    {
        Entity = owningEntity;
    }

    public Entity Entity { get; internal set; }

    public Transform Parent => Entity.Parent?.Transform;

    #region Debug drawing

    internal void DebugDraw()
    {
        Core.SpriteBatch.DrawPoint(
            WorldPosition2D,
            Color.Red,
            3f * Scene.Instance.MainCamera
                .WorldUnitsPerScreenPixel);
    }

    #endregion

    #region Local state

    /// <summary>
    ///     Position relative to the parent transform.
    /// </summary>
    public Vector3 Position { get; set; } = Vector3.Zero;

    /// <summary>
    ///     Rotation relative to the parent transform.
    /// </summary>
    public Quaternion Rotation
    {
        get => _rotation;
        set => _rotation = NormalizeQuaternion(value);
    }

    /// <summary>
    ///     Scale relative to the parent transform.
    /// </summary>
    public Vector3 Scale { get; set; } = Vector3.One;

    #endregion

    #region World state

    public Vector3 WorldPosition
    {
        get
        {
            if (Parent == null)
                return Position;

            return Vector3.Transform(
                Position,
                Parent.WorldMatrix);
        }

        set
        {
            if (Parent == null)
            {
                Position = value;
                return;
            }

            var inverseParent = InvertMatrix(
                Parent.WorldMatrix,
                "Cannot set world position because the parent transform is not invertible.");

            Position = Vector3.Transform(value, inverseParent);
        }
    }

    public Quaternion WorldRotation
    {
        get
        {
            if (Parent == null)
                return Rotation;

            /*
             * World matrices use:
             *
             *     local * parent
             *
             * Build the rotation in the same order so that quaternion
             * multiplication conventions cannot introduce ambiguity.
             */
            var worldRotationMatrix =
                Matrix.CreateFromQuaternion(Rotation) *
                Matrix.CreateFromQuaternion(Parent.WorldRotation);

            return NormalizeQuaternion(
                Quaternion.CreateFromRotationMatrix(worldRotationMatrix));
        }

        set
        {
            var desiredWorldRotation =
                NormalizeQuaternion(value);

            if (Parent == null)
            {
                Rotation = desiredWorldRotation;
                return;
            }

            var desiredWorldMatrix =
                Matrix.CreateFromQuaternion(desiredWorldRotation);

            var parentRotationMatrix =
                Matrix.CreateFromQuaternion(Parent.WorldRotation);

            /*
             * Rotation matrices are orthonormal, so their transpose is
             * also their inverse.
             *
             * local * parent = world
             * local = world * inverse(parent)
             */
            var localRotationMatrix =
                desiredWorldMatrix *
                Matrix.Transpose(parentRotationMatrix);

            Rotation = Quaternion.CreateFromRotationMatrix(
                localRotationMatrix);
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

        set
        {
            if (Parent == null)
            {
                Scale = value;
                return;
            }

            Scale = DivideScale(value, Parent.WorldScale);
        }
    }

    #endregion

    #region Matrices

    public Matrix LocalMatrix =>
        Matrix.CreateScale(Scale) *
        Matrix.CreateFromQuaternion(Rotation) *
        Matrix.CreateTranslation(Position);

    public Matrix WorldMatrix
    {
        get
        {
            if (Parent == null)
                return LocalMatrix;

            return LocalMatrix * Parent.WorldMatrix;
        }
    }

    #endregion

    #region 3D axes

    /*
     * Forward = +X
     * Right   = +Y
     * Up      = +Z
     */

    public Vector3 Forward =>
        Vector3.Transform(Vector3.UnitX, WorldRotation);

    public Vector3 Right =>
        Vector3.Transform(Vector3.UnitY, WorldRotation);

    public Vector3 Up =>
        Vector3.Transform(Vector3.UnitZ, WorldRotation);

    public Vector3 LocalForward =>
        Vector3.Transform(Vector3.UnitX, Rotation);

    public Vector3 LocalRight =>
        Vector3.Transform(Vector3.UnitY, Rotation);

    public Vector3 LocalUp =>
        Vector3.Transform(Vector3.UnitZ, Rotation);

    #endregion

    #region 2D state

    /// <summary>
    ///     Local position projected onto the XY plane.
    ///     Setting it preserves the current local Z position.
    /// </summary>
    public Vector2 Position2D
    {
        get => new(Position.X, Position.Y);

        set => Position = new Vector3(
            value.X,
            value.Y,
            Position.Z);
    }

    /// <summary>
    ///     World position projected onto the XY plane.
    ///     Setting it preserves the current world Z position.
    /// </summary>
    public Vector2 WorldPosition2D
    {
        get => new(WorldPosition.X, WorldPosition.Y);

        set => WorldPosition = new Vector3(
            value.X,
            value.Y,
            WorldPosition.Z);
    }

    /// <summary>
    ///     Local scale on the XY plane.
    ///     Setting it preserves local Z scale.
    /// </summary>
    public Vector2 Scale2D
    {
        get => new(Scale.X, Scale.Y);

        set => Scale = new Vector3(
            value.X,
            value.Y,
            Scale.Z);
    }

    /// <summary>
    ///     World scale on the XY plane.
    ///     Setting it preserves world Z scale.
    /// </summary>
    public Vector2 WorldScale2D
    {
        get => new(WorldScale.X, WorldScale.Y);

        set => WorldScale = new Vector3(
            value.X,
            value.Y,
            WorldScale.Z);
    }

    /// <summary>
    ///     Local rotation around the Z axis, in radians.
    ///     Setting this replaces the local rotation with a pure 2D
    ///     rotation around Z.
    /// </summary>
    public float Rotation2D
    {
        get => GetXYAngle(LocalForward);

        set => Rotation = Quaternion.CreateFromAxisAngle(
            Vector3.UnitZ,
            value);
    }

    /// <summary>
    ///     World rotation around the Z axis, in radians.
    ///     Setting this replaces the world rotation with a pure 2D
    ///     rotation around Z.
    /// </summary>
    public float WorldRotation2D
    {
        get => GetXYAngle(Forward);

        set => WorldRotation = Quaternion.CreateFromAxisAngle(
            Vector3.UnitZ,
            value);
    }

    public Vector2 Forward2D =>
        NormalizeOrFallback(
            new Vector2(Forward.X, Forward.Y),
            Vector2.UnitX);

    public Vector2 Right2D =>
        NormalizeOrFallback(
            new Vector2(Right.X, Right.Y),
            Vector2.UnitY);

    public Vector2 LastWorldPosition2D =>
        new(LastWorldPosition.X, LastWorldPosition.Y);

    [Obsolete(
        "Rotation is independent of camera scale. Use WorldRotation2D instead.")]
    public float ScaledZRotation =>
        WorldRotation2D;

    #endregion

    #region Rotation helpers

    /// <summary>
    ///     Sets local Euler rotation in radians.
    ///     X = pitch
    ///     Y = yaw
    ///     Z = roll
    /// </summary>
    public void SetEulerRotation(Vector3 radians)
    {
        SetEulerRotation(
            radians.X,
            radians.Y,
            radians.Z);
    }

    /// <summary>
    ///     Sets local Euler rotation in radians.
    /// </summary>
    public void SetEulerRotation(
        float pitch,
        float yaw,
        float roll)
    {
        Rotation = Quaternion.CreateFromYawPitchRoll(
            yaw,
            pitch,
            roll);
    }

    /// <summary>
    ///     Sets world Euler rotation in radians.
    /// </summary>
    public void SetWorldEulerRotation(Vector3 radians)
    {
        SetWorldEulerRotation(
            radians.X,
            radians.Y,
            radians.Z);
    }

    /// <summary>
    ///     Sets world Euler rotation in radians.
    /// </summary>
    public void SetWorldEulerRotation(
        float pitch,
        float yaw,
        float roll)
    {
        WorldRotation = Quaternion.CreateFromYawPitchRoll(
            yaw,
            pitch,
            roll);
    }

    /// <summary>
    ///     Rotates around the local Z axis.
    ///     Intended for 2D entities.
    /// </summary>
    public void Rotate2D(float radians)
    {
        Rotation2D += radians;
    }

    /// <summary>
    ///     Rotates around the world Z axis.
    ///     Intended for 2D entities.
    /// </summary>
    public void RotateWorld2D(float radians)
    {
        WorldRotation2D += radians;
    }

    public void RotateTowards2D(
        float targetWorldAngle,
        float maxRadiansDelta)
    {
        var currentAngle = WorldRotation2D;

        // Returns the shortest signed angle from current to target,
        // wrapped between -Pi and +Pi.
        var angleDifference = MathHelper.WrapAngle(
            targetWorldAngle - currentAngle);

        var rotationAmount = MathHelper.Clamp(
            angleDifference,
            -maxRadiansDelta,
            maxRadiansDelta);

        WorldRotation2D = currentAngle + rotationAmount;
    }

    public void RotateTowards2D(
        Vector2 worldDirection,
        float maxRadiansDelta)
    {
        if (worldDirection.LengthSquared() <= 0.000001f)
            return;

        var targetAngle = MathF.Atan2(
            worldDirection.Y,
            worldDirection.X);

        RotateTowards2D(
            targetAngle,
            maxRadiansDelta);
    }

    public void RotateTowardsPoint2D(
        Vector2 worldTarget,
        float maxRadiansDelta)
    {
        var direction =
            worldTarget - WorldPosition2D;

        RotateTowards2D(
            direction,
            maxRadiansDelta);
    }

    #endregion

    #region Look-at methods

    /// <summary>
    ///     Points local +X toward a world-space target.
    ///     Local +Z is kept as close as possible to worldUp.
    /// </summary>
    public void LookAt(
        Vector3 worldTarget,
        Vector3? worldUp = null)
    {
        var direction =
            worldTarget - WorldPosition;

        LookInDirection(
            direction,
            worldUp ?? Vector3.UnitZ);
    }

    /// <summary>
    ///     Points local +X in a world-space direction.
    /// </summary>
    public void LookInDirection(
        Vector3 worldDirection,
        Vector3? worldUp = null)
    {
        if (worldDirection.LengthSquared() <= Epsilon)
            return;

        var forward =
            Vector3.Normalize(worldDirection);

        var upReference =
            worldUp ?? Vector3.UnitZ;

        if (upReference.LengthSquared() <= Epsilon)
            upReference = Vector3.UnitZ;

        upReference.Normalize();

        /*
         * If forward is nearly parallel to the requested up vector,
         * their cross product cannot produce a stable right vector.
         */
        if (MathF.Abs(Vector3.Dot(forward, upReference)) > 0.999f)
            upReference =
                MathF.Abs(Vector3.Dot(forward, Vector3.UnitZ)) < 0.999f
                    ? Vector3.UnitZ
                    : Vector3.UnitY;

        /*
         * Axis convention:
         *
         * forward x right = up
         *
         * Therefore:
         *
         * right = up x forward
         */
        var right = Vector3.Normalize(
            Vector3.Cross(upReference, forward));

        var correctedUp = Vector3.Normalize(
            Vector3.Cross(forward, right));

        /*
         * MonoGame's row-vector transformation convention means each
         * transformed local basis vector occupies a matrix row.
         *
         * UnitX -> Forward
         * UnitY -> Right
         * UnitZ -> Up
         */
        Matrix rotationMatrix = new(
            forward.X, forward.Y, forward.Z, 0f,
            right.X, right.Y, right.Z, 0f,
            correctedUp.X, correctedUp.Y, correctedUp.Z, 0f,
            0f, 0f, 0f, 1f);

        WorldRotation = Quaternion.CreateFromRotationMatrix(
            rotationMatrix);
    }

    public void LookAt2D(Vector2 worldTarget)
    {
        LookInDirection2D(
            worldTarget - WorldPosition2D);
    }

    public void LookInDirection2D(Vector2 worldDirection)
    {
        if (worldDirection.LengthSquared() <= Epsilon)
            return;

        WorldRotation2D = MathF.Atan2(
            worldDirection.Y,
            worldDirection.X);
    }

    #endregion

    #region Translation methods

    /// <summary>
    ///     Adds a translation in the transform's parent coordinate space.
    /// </summary>
    public void Translate(Vector3 translation)
    {
        Position += translation;
    }

    /// <summary>
    ///     Adds an XY translation in the transform's parent coordinate space.
    /// </summary>
    public void Translate2D(Vector2 translation)
    {
        Position += new Vector3(
            translation.X,
            translation.Y,
            0f);
    }

    public void TranslateWorld(Vector3 translation)
    {
        WorldPosition += translation;
    }

    public void TranslateWorld2D(Vector2 translation)
    {
        WorldPosition += new Vector3(
            translation.X,
            translation.Y,
            0f);
    }

    public void MoveForward(float distance)
    {
        WorldPosition += Forward * distance;
    }

    public void MoveRight(float distance)
    {
        WorldPosition += Right * distance;
    }

    public void MoveUp(float distance)
    {
        WorldPosition += Up * distance;
    }

    public void MoveForward2D(float distance)
    {
        TranslateWorld2D(
            Forward2D * distance);
    }

    public void MoveRight2D(float distance)
    {
        TranslateWorld2D(
            Right2D * distance);
    }

    #endregion

    #region Point conversion

    /// <summary>
    ///     Converts a local-space point into world space.
    ///     Includes position, rotation, and scale.
    /// </summary>
    public Vector3 TransformPoint(Vector3 localPoint)
    {
        return Vector3.Transform(
            localPoint,
            WorldMatrix);
    }

    /// <summary>
    ///     Converts a world-space point into local space.
    ///     Includes position, rotation, and scale.
    /// </summary>
    public Vector3 InverseTransformPoint(Vector3 worldPoint)
    {
        var inverseWorld = InvertMatrix(
            WorldMatrix,
            "Cannot inverse-transform the point because the world transform is not invertible.");

        return Vector3.Transform(
            worldPoint,
            inverseWorld);
    }

    public Vector2 TransformPoint2D(Vector2 localPoint)
    {
        var result = TransformPoint(
            new Vector3(localPoint, 0f));

        return new Vector2(
            result.X,
            result.Y);
    }

    /// <summary>
    ///     Converts an XY world position into local XY coordinates.
    ///     Assumes the world point lies on the transform's current world-Z plane.
    /// </summary>
    public Vector2 InverseTransformPoint2D(Vector2 worldPoint)
    {
        return InverseTransformPoint2D(
            worldPoint,
            WorldPosition.Z);
    }

    public Vector2 InverseTransformPoint2D(
        Vector2 worldPoint,
        float worldZ)
    {
        var result = InverseTransformPoint(
            new Vector3(
                worldPoint.X,
                worldPoint.Y,
                worldZ));

        return new Vector2(
            result.X,
            result.Y);
    }

    #endregion

    #region Direction conversion

    /// <summary>
    ///     Converts a local direction into world space.
    ///     Ignores position and scale.
    /// </summary>
    public Vector3 TransformDirection(Vector3 localDirection)
    {
        return Vector3.Transform(
            localDirection,
            WorldRotation);
    }

    /// <summary>
    ///     Converts a world direction into local space.
    ///     Ignores position and scale.
    /// </summary>
    public Vector3 InverseTransformDirection(Vector3 worldDirection)
    {
        return Vector3.Transform(
            worldDirection,
            Quaternion.Inverse(WorldRotation));
    }

    public Vector2 TransformDirection2D(Vector2 localDirection)
    {
        var result = TransformDirection(
            new Vector3(localDirection, 0f));

        return new Vector2(
            result.X,
            result.Y);
    }

    public Vector2 InverseTransformDirection2D(Vector2 worldDirection)
    {
        var result = InverseTransformDirection(
            new Vector3(worldDirection, 0f));

        return new Vector2(
            result.X,
            result.Y);
    }

    #endregion

    #region Physics snapshot

    /// <summary>
    ///     Captures the current world position for physics interpolation,
    ///     swept collision checks, or velocity calculations.
    ///     Call this before changing transforms for the current physics step.
    /// </summary>
    internal void CaptureLastWorldPosition()
    {
        LastWorldPosition = WorldPosition;
    }

    /// <summary>
    ///     Prevents a newly created or teleported object from appearing
    ///     to have moved from an old position.
    /// </summary>
    internal void ResetLastWorldPosition()
    {
        LastWorldPosition = WorldPosition;
    }

    #endregion

    #region Helpers

    private static Quaternion NormalizeQuaternion(
        Quaternion quaternion)
    {
        if (quaternion.LengthSquared() <= Epsilon)
            return Quaternion.Identity;

        return Quaternion.Normalize(quaternion);
    }

    private static Vector2 NormalizeOrFallback(
        Vector2 vector,
        Vector2 fallback)
    {
        if (vector.LengthSquared() <= Epsilon)
            return fallback;

        return Vector2.Normalize(vector);
    }

    private static float GetXYAngle(Vector3 forward)
    {
        Vector2 projectedForward = new(
            forward.X,
            forward.Y);

        if (projectedForward.LengthSquared() <= Epsilon)
            return 0f;

        return MathF.Atan2(
            projectedForward.Y,
            projectedForward.X);
    }

    private static Vector3 DivideScale(
        Vector3 desiredWorldScale,
        Vector3 parentWorldScale)
    {
        return new Vector3(
            DivideScaleComponent(
                desiredWorldScale.X,
                parentWorldScale.X),
            DivideScaleComponent(
                desiredWorldScale.Y,
                parentWorldScale.Y),
            DivideScaleComponent(
                desiredWorldScale.Z,
                parentWorldScale.Z));
    }

    private static float DivideScaleComponent(
        float desiredScale,
        float parentScale)
    {
        if (MathF.Abs(parentScale) <= Epsilon)
            throw new InvalidOperationException(
                "Cannot calculate local scale because a parent scale component is zero.");

        return desiredScale / parentScale;
    }

    private static Matrix InvertMatrix(
        Matrix matrix,
        string errorMessage)
    {
        if (MathF.Abs(matrix.Determinant()) <= Epsilon)
            throw new InvalidOperationException(errorMessage);

        return Matrix.Invert(matrix);
    }

    #endregion
}
