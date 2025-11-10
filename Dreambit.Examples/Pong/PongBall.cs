using System;
using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit.Examples.Pong;

[Require(typeof(BoxCollider), typeof(RectDrawer))]
public class PongBall : Component<PongBall>
{
    private BoxCollider _collider;
    private RectDrawer _rectDrawer;

    private Vector2 _direction;
    private float _velocity = 256.0f;
    
    public override void OnCreated()
    {
        _collider = Entity.GetComponent<BoxCollider>();
        _rectDrawer = Entity.GetComponent<RectDrawer>();

        _rectDrawer.PivotType = PivotType.Center;
        _rectDrawer.Width = 16;
        _rectDrawer.Height = 16;
        _rectDrawer.Color = Color.White;

        _direction = GetRandomDirection();
    }

    public override void OnUpdate()
    {
        //store the old position
        var oldPosition = Transform.Position;
        Transform.Position += (_direction.ToVector3() * _velocity * Time.DeltaTime);
        
        //check if we are out of bounds
        if (!IsWithinVerticalBounds())
        {
            Transform.Position = oldPosition;
            _direction.Y *= -1.0f;
        }

        if (!IsWithinHorizontalBounds())
        {
            Scene.SetNextScene(new PongScene());
        }

        //check for collision & move back if we collided / change the direction
        if (CheckForCollision(out var result))
        {
            Transform.Position = oldPosition;
            _direction.X *= -1.0f;
        }
        
        
    }

    /// <summary>
    // Returns true if we are within bounds
    /// </summary>
    /// <returns></returns>
    private bool IsWithinVerticalBounds()
    {
        if (Transform.Position.Y < 0)
            return false;

        if (Transform.Position.Y > PongSettings.GameHeight)
            return false;

        return true;
    }

    private bool IsWithinHorizontalBounds()
    {
        if (Transform.Position.X < 0)
        {
            PongSettings.PlayerTwoScore += 1;
            return false;
        }

        if (Transform.Position.X > PongSettings.GameWidth)
        {
            PongSettings.PlayerOneScore += 1;
            return false;
        }

        return true;
    }

    private bool CheckForCollision(out CollisionResult result)
    {
        return PhysicsSystem.Instance.ColliderCastByTag(_collider, out result, ["paddle"]);
    }

    private Vector2 GetRandomDirection()
    {
        while (true)
        {
            var random = new Random();

            var randX = random.Next(-1.0f, 1.0f);
            var randY = random.Next(-1.0f, 1.0f);

            var direction = new Vector2(randX, randY);

            if (direction == Vector2.Zero) continue;

            direction.Normalize();

            return direction;
        }
    }
}