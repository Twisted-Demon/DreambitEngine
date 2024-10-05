using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox.Components;

[Require(typeof(AnimatedSprite))]
public class SandboxController : Component
{
    private Vector3 _moveDir;
    private const float Speed = 75f;
    
    //animations
    private SpriteSheetAnimation _idleAnimation;
    private SpriteSheetAnimation _runAnimation;

    private AnimatedSprite _animator;

    public override void OnCreated()
    {
        _idleAnimation = Resources.Load<SpriteSheetAnimation>("Animations/b_witch_idle");
        _runAnimation = Resources.Load<SpriteSheetAnimation>("Animations/b_witch_run");

        _animator = Entity.GetComponent<AnimatedSprite>();
        _animator.Play();
    }

    public override void OnUpdate()
    {
        HandleMovement();
        UpdateAnimation();
        
        if(Input.IsKeyPressed(Keys.Escape))
            Core.Instance.Exit();
    }

    private void UpdateAnimation()
    {
        _animator.SpriteSheetAnimation = 
            _moveDir == Vector3.Zero ? _idleAnimation : _runAnimation;
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
        
        Transform.Position += _moveDir * Speed * Time.DeltaTime;
    }
}