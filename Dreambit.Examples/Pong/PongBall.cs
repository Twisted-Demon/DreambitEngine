using System;
using System.Collections;
using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit.Examples.Pong;

[Require(typeof(BoxCollider), typeof(RectDrawer))]
public class PongBall : Component
{
    private BoxCollider _collider;
    private RectDrawer _rectDrawer;

    private Vector2 _direction;
    private float _velocity = 326.0f;
    
    public new PongScene Scene => (PongScene)base.Scene;
    
    public override void OnCreated()
    {
        _collider = Entity.GetComponent<BoxCollider>();
        _rectDrawer = Entity.GetComponent<RectDrawer>();

        _rectDrawer.PivotType = PivotType.Center;
        _rectDrawer.Width = 16;
        _rectDrawer.Height = 16;
        _rectDrawer.Color = Color.White;

        CoroutineService.StartCoroutine(WaitToStartBall());
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
            Dreambit.Scene.SetNextScene<PongScene>();
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
            Scene.IncrementPlayerTwoScore();
            return false;
        }

        if (Transform.Position.X > PongSettings.GameWidth)
        {
            Scene.IncrementPlayerOneScore();
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
            var randX = Random.Shared.NextSingle() * 2f - 1f;
            var randY = Random.Shared.NextSingle() * 2f - 1f;

            var direction = new Vector2(randX, randY);

            if (direction == Vector2.Zero) continue;

            direction.Normalize();

            return direction;
        }
    }

    private IEnumerator WaitToStartBall()
    {
        yield return new WaitForSeconds(2.5f);
        _direction = GetRandomDirection();
    }
}
