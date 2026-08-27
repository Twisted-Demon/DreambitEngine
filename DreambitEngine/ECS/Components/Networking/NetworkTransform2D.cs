using Dreambit.Networking;
using Dreambit.Networking.Replication;
using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

[BlueprintType(nameof(NetworkTransform2D))]
[NetworkReplicated((ushort)ReplicationId.NetworkTransform2D)]
public class NetworkTransform2D : Component
{
    private Collider? _collider;
    private bool _clientPoseInitialized;
    
    [Replicated(1)]
    public Vector2 AuthoritativePosition { get; set; }

    [Replicated(2)]
    public float AuthoritativeRotation { get; set; }

    [Replicated(3)]
    public Vector2 AuthoritativeScale { get; set; } = Vector2.One;
    
    /// <summary>
    /// When false, the ordinary remote-transform presentation is skipped
    /// for the locally owned entity. Client prediction can then control
    /// the local Transform and reconcile separately.
    /// </summary>
    [DreambitSerialize]
    [Tooltip("When false, the ordinary remote-transform presentation is skipped for the locally owned entity. " +
             "Client prediction can then control the local Transform and reconcile separately")]
    public bool ApplyToLocalOwner { get; set; } = true;

    [DreambitSerialize]
    public float PositionSharpness { get; set; } = 20f;

    [DreambitSerialize]
    public float RotationSharpness { get; set; } = 20f;

    [DreambitSerialize]
    public float ScaleSharpness { get; set; } = 20f;
    
    /// <summary>
    /// Errors larger than this are treated as teleports instead of
    /// interpolated motion.
    /// </summary>
    [DreambitSerialize]
    [Tooltip("Errors larger than this are treated as teleports instead of interpolated motion")]
    public float SnapDistance { get; set; } = 3f;

    public override void OnAddedToEntity()
    {
        _collider = Entity.GetComponent<Collider>();

        if (Core.Instance.Networking.IsServer)
            CaptureAuthoritativeTransform();
    }
    
    public override void OnUpdate()
    {
        var network = Core.Instance.Networking;

        if (network.IsServer)
        {
            CaptureAuthoritativeTransform();
            return;
        }

        if (network.Role != NetworkRole.Client)
            return;

        if (!ApplyToLocalOwner &&
            network.IsOwnedByLocalPeer(Entity))
        {
            return;
        }

        ApplyRemoteTransform();
    }
    
    private void CaptureAuthoritativeTransform()
    {
        AuthoritativePosition = Transform.WorldPosition2D;
        AuthoritativeRotation = Transform.WorldRotation2D;
        AuthoritativeScale = Transform.WorldScale2D;
    }
    
    private void ApplyRemoteTransform()
    {
        if (!_clientPoseInitialized)
        {
            SetWorldPose(
                AuthoritativePosition,
                AuthoritativeRotation,
                AuthoritativeScale);

            _clientPoseInitialized = true;
            return;
        }

        var currentPosition = Transform.WorldPosition2D;
        var positionDelta =
            AuthoritativePosition - currentPosition;

        var snapDistance =
            Mathf.Max(0f, SnapDistance);

        if (positionDelta.LengthSquared() >
            snapDistance * snapDistance)
        {
            SetWorldPose(
                AuthoritativePosition,
                AuthoritativeRotation,
                AuthoritativeScale);

            return;
        }

        var positionT =
            CalculateSharpnessFactor(PositionSharpness);

        var rotationT =
            CalculateSharpnessFactor(RotationSharpness);

        var scaleT =
            CalculateSharpnessFactor(ScaleSharpness);

        var position =
            Vector2.Lerp(
                currentPosition,
                AuthoritativePosition,
                positionT);

        var currentRotation =
            Transform.WorldRotation2D;

        var rotationDelta =
            MathHelper.WrapAngle(
                AuthoritativeRotation -
                currentRotation);

        var rotation =
            currentRotation +
            rotationDelta * rotationT;

        var scale =
            Vector2.Lerp(
                Transform.WorldScale2D,
                AuthoritativeScale,
                scaleT);

        SetWorldPose(
            position,
            rotation,
            scale);
    }
    
    private void SetWorldPose(
        Vector2 position,
        float rotation,
        Vector2 scale)
    {
        Transform.WorldPosition2D = position;
        Transform.WorldRotation2D = rotation;
        Transform.WorldScale2D = scale;

        // NetworkTransform lives inside DreambitEngine, so it can keep the
        // collider's spatial-hash representation consistent with the new pose.
        _collider ??= Entity.GetComponent<Collider>();
        _collider?.RefreshSpatialHash();
    }
    
    private static float CalculateSharpnessFactor(
        float sharpness)
    {
        if (sharpness <= 0f ||
            Time.DeltaTime <= 0f)
        {
            return 0f;
        }

        return 1f -
               Mathf.Exp(
                   -sharpness *
                   Time.DeltaTime);
    }
}