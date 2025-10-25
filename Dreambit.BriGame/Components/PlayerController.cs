using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Dreambit.BriGame.Components;

[Require(typeof(SpriteAnimator), typeof(Mover))]
public class PlayerController : Component
{
    private Logger<PlayerController> _logger = new Logger<PlayerController>();
    
    //animations
    private SpriteSheetAnimation _idleAnimation;
    
    private SpriteAnimator _animator;
    private SpriteDrawer _spriteDrawer;
    
    //movement
    private Vector3 _moveDir;
    private const float Speed = 75f;
    
    private Mover _mover;
    

    public override void OnUpdate()
    {
        HandleMovement();
    }

    public override void OnAddedToEntity()
    {
        _idleAnimation = Resources.LoadAsset<SpriteSheetAnimation>("Animations/bri_idle");
        
        _animator = Entity.GetComponent<SpriteAnimator>();
        _animator.Animation = _idleAnimation;
        _animator.Play();
        _spriteDrawer = Entity.GetComponent<SpriteDrawer>();

        _mover = Entity.GetComponent<Mover>();

        Scene.MainCamera.IsFollowing = true;
        Scene.MainCamera.TransformToFollow = Transform;
        Scene.MainCamera.CameraFollowBehavior = CameraFollowBehavior.Lerp;
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