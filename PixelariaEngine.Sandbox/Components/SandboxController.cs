using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PixelariaEngine.ECS;
using PixelariaEngine.Graphics;

namespace PixelariaEngine.Sandbox.Components;

[Require(typeof(SpriteAnimator))]
public class SandboxController : Component<SandboxController>
{
    private Vector3 _moveDir;
    private Vector2 _facing;
    private const float Speed = 75f;
    
    //animations
    private SpriteSheetAnimation _idleAnimation;
    private SpriteSheetAnimation _runAnimation;

    private SpriteAnimator _animator;
    private SpriteDrawer _sprite;
    
    private BoxCollider _collider;

    public override void OnCreated()
    {
        _idleAnimation = Resources.Load<SpriteSheetAnimation>("Animations/b_witch_idle");
        _runAnimation = Resources.Load<SpriteSheetAnimation>("Animations/b_witch_run");

        _animator = Entity.GetComponent<SpriteAnimator>();
        _animator.Play();
        
        _sprite = Entity.GetComponent<SpriteDrawer>();
        
        Scene.MainCamera.IsFollowing = true;
        Scene.MainCamera.TransformToFollow = Transform;
        Scene.MainCamera.CameraFollowBehavior = CameraFollowBehavior.Lerp;
        
        _collider = Entity.GetComponentInChildren<BoxCollider>();
        
        TestUi();
    }

    private void TestUi()
    {
        var (canvas, canvasEntity) = Canvas.Create();

        var text = UIText.Create(canvas, "Hello World");
        
        text.Transform.Position = new Vector3(25, 0, 0);
    }

    public override void OnUpdate()
    {
        HandleMovement();
        UpdateAnimation();
        
        if(Input.IsKeyPressed(Keys.Escape))
            Core.Instance.Exit();

        if (Input.IsKeyPressed(Keys.F1))
            Scene.DebugMode = !Scene.DebugMode;
    }

    private void UpdateAnimation()
    {
        _animator.Animation = 
            _moveDir == Vector3.Zero ? _idleAnimation : _runAnimation;

        if (!(_moveDir.Length() > 0)) return;
        
        _facing = new Vector2(_moveDir.X, _moveDir.Y);

        switch (_facing.X)
        {
            case 0:
                return;
            case < 0:
                _sprite.IsHorizontalFlip = true;
                break;
            default:
                _sprite.IsHorizontalFlip = false;
                break;
        }
    }

    private void HandleMovement()
    {
        _moveDir = Vector3.Zero;
        
        _moveDir.X += Input.IsKeyHeld(Keys.D) ? 1 : 0;
        _moveDir.X -= Input.IsKeyHeld(Keys.A)  ? 1 : 0;
        
        _moveDir.Y += Input.IsKeyHeld(Keys.S)  ? 1 : 0;
        _moveDir.Y -= Input.IsKeyHeld(Keys.W)  ? 1 : 0;
        
        if(_moveDir != Vector3.Zero)
            _moveDir.Normalize();

        if (Input.IsKeyPressed(Keys.I))
        {
            if (PhysicsSystem.Instance.Cast(_collider, out var collisionResult))
            {
                Logger.Debug($"We are colliding");
            }
        }
        
        Transform.Position += _moveDir * Speed * Time.DeltaTime;
    }
}