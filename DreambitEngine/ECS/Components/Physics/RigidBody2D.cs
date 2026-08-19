using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

[BlueprintType(nameof(RigidBody2D))]
public class RigidBody2D : Component
{
    #region Private Members / Fields

    private bool _warnedUser;

    #endregion

    #region Life Cycle Overrides

    public override void OnPhysicsUpdate()
    {
        if (Collider is null)
        {
            if (_warnedUser)
                return;

            Logger.Warn("Collider is null!");
            _warnedUser = true;

            return;
        }

        Transform.CaptureLastWorldPosition();

        Transform.TranslateWorld2D(
            Velocity *
            Time.PhysicsDeltaTime);

        /*
         * Keep the broadphase current immediately after moving.
         *
         * Collider itself may also receive OnPhysicsUpdate during this physics
         * step, but its world-geometry cache makes that later refresh a cheap
         * no-op when nothing changed.
         */
        Collider.RefreshSpatialHash();

        /*
         * This uses a boolean-only physics query and therefore does not allocate
         * CollisionResult/List storage every rigidbody step.
         */
        if (!CheckForCollision())
            return;

        // Restore the previous position if the movement overlapped something.
        Transform.WorldPosition =
            Transform.LastWorldPosition;

        Collider.RefreshSpatialHash();
    }

    #endregion

    #region Internal Helper Functions

    private bool CheckForCollision()
    {
        if (InterestedTags.Count == 0)
        {
            return PhysicsSystem.Instance
                .ColliderCastAny(Collider);
        }

        return PhysicsSystem.Instance
            .ColliderCastAnyByTag(
                Collider,
                InterestedTags);
    }

    #endregion

    #region Public Properties / Fields

    [DreambitSerialize]
    public Collider Collider { get; private set; }

    [DreambitSerialize]
    public Vector2 Velocity = Vector2.Zero;

    [DreambitSerialize]
    public HashSet<string> InterestedTags { get; private set; } = [];

    #endregion

    #region Public Functions

    public void SetInterestedTags(
        params string[] tags)
    {
        foreach (var tag in tags)
            InterestedTags.Add(tag);
    }

    public void SetCollider(
        Collider collider)
    {
        Collider = collider;
        _warnedUser = false;
    }

    #endregion
}