using System;
using Dreambit.BriGame.Scenes;
using Dreambit.ECS;
using Dreambit.ECS.Audio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;

namespace Dreambit.BriGame.Components;

[Require(typeof(SpriteAnimator), typeof(Mover), typeof(SpriteDrawer))]
public class PlayerController : Component
{
    private Logger<PlayerController> _logger = new Logger<PlayerController>();
    
    //animations
    
    private SpriteAnimator _animator;
    private SpriteDrawer _spriteDrawer;
    
    //movement
    private Vector3 _moveDir;
    private const float Speed = 75f;
    
    private Mover _mover;
    
    private BoxCollider _boxCollider;
    

    public override void OnUpdate()
    {
        HandleMovement();

        if (Input.IsKeyPressed(Keys.Space))
            Scene.SetNextLDtkScene<BriWorldScene>(Worlds.BriWorld.Level_0);
    }

    public override void OnCreated()
    {
        _animator = Entity.GetComponent<SpriteAnimator>();
        _animator.SetAnimation("Animations/bri_idle");
        _animator.Play();
        _spriteDrawer = Entity.GetComponent<SpriteDrawer>();
        
        _mover = Entity.GetComponent<Mover>();

        Scene.MainCamera.IsFollowing = true;
        Scene.MainCamera.TransformToFollow = Transform;
        Scene.MainCamera.CameraFollowBehavior = CameraFollowBehavior.Lerp;

    }

    public override void OnAddedToEntity()
    {
        _boxCollider = Entity.GetComponentInChildren<BoxCollider>();
        _boxCollider.OnCollisionEnter += OnCollisionEnter;
        _boxCollider.OnCollisionExit += OnCollisionExit;
    }

    private void OnCollisionExit(Collider other)
    {
        if (other.Entity.Tags.Contains("foliage"))
        {
            var drawer = other.Entity.Parent.GetComponent<SpriteDrawer>();
            drawer.WithOpacity(1.0f);
        }
    }

    private void OnCollisionEnter(Collider other)
    {
        if (other.Entity.Tags.Contains("foliage"))
        {
            var drawer = other.Entity.Parent.GetComponent<SpriteDrawer>();
            drawer.WithOpacity(0.25f);
        }
    }
    

    private void HandleMovement()
    {
        _moveDir = Vector3.Zero;

        _moveDir.X += Input.IsKeyHeld(Keys.D) ? 1 : 0;
        _moveDir.X -= Input.IsKeyHeld(Keys.A) ? 1 : 0;
        
        _moveDir.Y += Input.IsKeyHeld(Keys.S)  ? 1 : 0;
        _moveDir.Y -= Input.IsKeyHeld(Keys.W)  ? 1 : 0;
        
        if(_moveDir != Vector3.Zero)
            _moveDir.Normalize();
        
        _mover.Velocity = _moveDir * Speed;
    }
}