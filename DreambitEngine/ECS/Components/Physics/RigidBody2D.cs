using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public class RigidBody2D : Component<RigidBody2D>
{
    #region Private Members / Fields

    private bool _warnedUser;

    #endregion

    #region Life Cycle Overrides

    public override void OnUpdate()
    {
        if (Collider is null)
        {
            if (_warnedUser) return;

            Logger.Warn("Collider is null!");
            _warnedUser = true;

            return;
        }

        var lastPosition = Transform.Position;
        Transform.Position += Velocity.ToVector3() * Time.DeltaTime;

        if (CheckForCollision(out _))
            // reset position if we did collide
            Transform.Position = lastPosition;
    }

    #endregion

    #region Internal Helper Functions

    private bool CheckForCollision(out CollisionResult result)
    {
        return InterestedTags.Count == 0
            ? PhysicsSystem.Instance.ColliderCast(Collider, out result)
            : PhysicsSystem.Instance.ColliderCastByTag(Collider, out result, InterestedTags.ToArray());
    }

    #endregion

    #region Public Properties / Fields

    public Collider Collider { get; private set; }

    public Vector2 Velocity = Vector2.Zero;

    public readonly HashSet<string> InterestedTags = [];

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