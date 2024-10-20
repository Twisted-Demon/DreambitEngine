using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PixelariaEngine.ECS;
using PixelariaEngine.Sandbox.Player;
using PixelariaEngine.Scripting;


namespace PixelariaEngine.Sandbox;

[Require(typeof(SpriteAnimator), typeof(TileWallMover))]
public class AriaController : Component
{
    private Logger<AriaController> _logger = new();
    private Vector3 _moveDir;
    private Vector2 _facing;
    private const float Speed = 75f;
    
    //animations
    private SpriteSheetAnimation _idleAnimation;
    private SpriteSheetAnimation _runAnimation;

    private SpriteAnimator _animator;
    private SpriteDrawer _sprite;
    
    private BoxCollider _collider;
    private TileWallMover _mover;
    
    private FSM _fsm;
    
    public override void OnAddedToEntity()
    {
        _idleAnimation = Resources.LoadAsset<SpriteSheetAnimation>("Animations/aria_idle");
        _runAnimation = Resources.LoadAsset<SpriteSheetAnimation>("Animations/aria_run");

        _animator = Entity.GetComponent<SpriteAnimator>();
        _animator.Play();
        
        _sprite = Entity.GetComponent<SpriteDrawer>();
        
        Scene.MainCamera.IsFollowing = true;
        Scene.MainCamera.TransformToFollow = Transform;
        Scene.MainCamera.CameraFollowBehavior = CameraFollowBehavior.Lerp;
        
        _collider = Entity.GetComponentInChildren<BoxCollider>();
        
        
        _mover = Entity.GetComponent<TileWallMover>();
        _mover.Collider = _collider;
        _mover.AstarGrid = Entity.FindByName("managers").GetComponent<AStarGrid>();

        _mover.InterestedTags = ["wall"];
        
        _fsm = Entity.GetComponent<FSM>();

        ScriptingManager.Instance.OnScriptingStart += () =>
        {
            _mover.Velocity = Vector3.Zero;
            _moveDir = Vector3.Zero;
            _mover.Enabled = false;
            _fsm.SetNextState<InDialogue>();
            Enabled = false;
        };
        
        ScriptingManager.Instance.OnScriptingEnd += () =>
        {
            Enabled = true;
            _mover.Enabled = true;
            _fsm.SetNextState<PlayerFree>();
        };
    }
    

    public override void OnUpdate()
    {
        HandleMovement();
        UpdateAnimation();
        
        if(Input.IsKeyPressed(Keys.Escape))
            Core.Instance.Exit();

        if (Input.IsKeyPressed(Keys.F1))
            Scene.DebugMode = !Scene.DebugMode;
        
        if(Input.IsKeyPressed(Keys.Space))
            Scene.SetNextLDtkScene(Worlds.AriaWorld.Dev_world);
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
        
        _moveDir.X += Input.IsKeyHeld(Keys.Right) ? 1 : 0;
        _moveDir.X -= Input.IsKeyHeld(Keys.Left)  ? 1 : 0;
        
        _moveDir.Y += Input.IsKeyHeld(Keys.Down)  ? 1 : 0;
        _moveDir.Y -= Input.IsKeyHeld(Keys.Up)    ? 1 : 0;
        
        if(_moveDir != Vector3.Zero)
            _moveDir.Normalize();
        
        _mover.Velocity = _moveDir * Speed;
    }
}