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
            if (_warnedUser) return;

            Logger.Warn("Collider is null!");
            _warnedUser = true;

            return;
        }

        Transform.CaptureLastWorldPosition();
        Transform.TranslateWorld2D(Velocity * Time.PhysicsDeltaTime);
        Collider.RefreshSpatialHash();

        if (CheckForCollision(out _))
        {
            // reset position if we did collide
            Transform.WorldPosition = Transform.LastWorldPosition;
            Collider.RefreshSpatialHash();
        }
    }

    #endregion

    #region Internal Helper Functions

    private bool CheckForCollision(out CollisionResult result)
    {
        return InterestedTags.Count == 0
            ? PhysicsSystem.Instance.ColliderCast(Collider, out result)
            : PhysicsSystem.Instance.ColliderCastByTag(Collider, out result, [.. InterestedTags]);
    }

    #endregion

    #region Public Properties / Fields

    [DreambitSerialize]
    public Collider Collider { get; private set; }

    [DreambitSerialize] public Vector2 Velocity = Vector2.Zero;

    [DreambitSerialize]
    public HashSet<string> InterestedTags { get; private set; } = [];

    #endregion

    #region Public Functions

    public void SetInterestedTags(params string[] tags)
    {
        foreach (var tag in tags) InterestedTags.Add(tag);
    }

    public void SetCollider(Collider collider)
    {
        Collider = collider;
        _warnedUser = false;
    }

    #endregion
}
