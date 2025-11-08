using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Dreambit.Examples.SpaceGame;



[Require(typeof(BoxCollider), typeof(SpriteDrawer), typeof(SpaceGameProjectileListener))]
public class SpaceGamePlayer : Component<SpaceGamePlayer>
{
    [FromRequired]
    private SpriteDrawer _drawer;
    
    [FromRequired]
    private BoxCollider _collider;
    
    [FromRequired]
    private SpaceGameProjectileListener _projectileListener;

    private const int ShipMiddleIndex = 1;
    private const int ShipLeftIndex = 0;
    private const int ShipRightIndex = 2;

    public float Velocity { get; set; }
    
    private Vector2 _inputDirection = Vector2.Zero;
    
    
    public override void OnCreated()
    {
        _projectileListener = Entity.GetComponent<SpaceGameProjectileListener>();
        _projectileListener.OnProjectileHit += HitByProjectile;

        var converters = BlueprintResolver.Instance.Converters;
    }

    public override void OnUpdate()
    {
        HandleMovementInput();
        MovePlayer();
        HandleFrameSwitching();
    }

    public override void OnDestroyed()
    {
        _projectileListener.OnProjectileHit -= HitByProjectile;
    }

    private void HandleMovementInput()
    {
        _inputDirection = Vector2.Zero;
        
        _inputDirection.X += Input.IsKeyHeld(Keys.A) ? -1 : 0;
        _inputDirection.X += Input.IsKeyHeld(Keys.D) ? 1 : 0;
        
        if (Input.IsKeyPressed(Keys.Space))
            SpaceGameProjectile.CreatePlayerBeam(Entity);
        
        if(Input.IsKeyPressed(Keys.Enter))
            Scene.SetNextScene<SpaceGameScene>();
    }

    private void MovePlayer()
    {
        var pos = Transform.Position;
        pos.X += _inputDirection.X * Velocity * Time.DeltaTime;

        const int minX = 0;
        const int maxX = SpaceGameSettings.GameWidth;
        pos.X = Mathf.Clamp(pos.X, minX, maxX);
        
        Transform.Position = pos;
    }

    private void HandleFrameSwitching()
    {
        switch (_inputDirection.X)
        {
            case < 0:
                _drawer.SetFrame(0); // left
                break;
            case > 0:
                _drawer.SetFrame(2); // right
                break;
            default:
                _drawer.SetFrame(1); // middle
                break;
        }
    }

    private void HitByProjectile()
    {
        
    }
}