using Dreambit.ECS;
using Dreambit.ECS.Audio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Dreambit.BriGame.Components.Galaga;

[Require(typeof(SpriteDrawer), typeof(SoundEffectEmitter))]
public class GalagaPlayer : Component
{
    public SpriteDrawer Drawer;

    public Vector3 IntendedDirection = Vector3.Zero;
    public SpriteSheet PlayerSpriteSheet;
    
    public SoundEffectEmitter SoundEmitter;
    public SoundCue PlayerShootSoundCue;

    private const int UpFrameIndex = 6;
    private const int LeftFrameIndex = 0;

    private const float Speed = 180f;

    public override void OnCreated()
    {
        Entity.AttachComponent<GalagaScore>();
    }

    public override void OnAddedToEntity()
    {
        Drawer = Entity.GetComponent<SpriteDrawer>();
        SoundEmitter = Entity.GetComponent<SoundEffectEmitter>();
        PlayerSpriteSheet = SpriteSheet.Create(7, 1, "Textures/Galaga/player_ship");
        Drawer.SpriteSheet = PlayerSpriteSheet;
        Drawer.SetFrame(UpFrameIndex);

        PlayerShootSoundCue = Resources.LoadAsset<SoundCue>("Sounds/Galaga/Cues/player_shoot");
    }

    public override void OnUpdate()
    {
        HandleInput();
        HandleMovement();
    }

    private void HandleInput()
    {
        IntendedDirection = Vector3.Zero;
        
        IntendedDirection.X += Input.IsKeyHeld(Keys.D) ? 1 : 0;
        IntendedDirection.X -= Input.IsKeyHeld(Keys.A) ? 1 : 0;
        
        
        HandleShoot();
    }

    private const float ShootCd = 0.25f;
    private float _shootTick = 0.0f;
    private bool _hasShot = false;
    
    private void HandleShoot()
    {
        if (_hasShot is true)
        {
            _shootTick += Time.DeltaTime;
            if (_shootTick >= ShootCd)
            {
                _hasShot = false;
                _shootTick = 0.0f;
            }
            else
            {
                return;
            }
        }
        
        if (Input.IsKeyPressed(Keys.Enter))
        {
            _hasShot = true;
            
            var pos = Transform.Position;
            pos.Y -= 8;
            pos.X -= 1;

            SoundEmitter.Play(PlayerShootSoundCue);
            
            ProjectileComponent
                .Create(pos, Vector3.Down, 280.0f, 4, Color.White, ["player_bullet"], Entity.Id);
        }
    }

    private void HandleMovement()
    {
        if (IntendedDirection == Vector3.Zero) return;
        
        var velocity = IntendedDirection * Speed * Time.DeltaTime;
        
        var intendedMovement = Transform.Position + velocity;
        
        if(intendedMovement.X is >= 0 and <= GalagaConstants.GameWidth)
            Transform.Position.X = intendedMovement.X;
    }

    public static GalagaPlayer SpawnPlayer()
    {
        const float spawnX = (float)GalagaConstants.GameWidth / 2;
        const float spawnY = (float)GalagaConstants.GameHeight - 24;
        var playerEntity = ECS.Entity.Create("player", createAt: new Vector3(spawnX, spawnY, 0));
        return playerEntity.AttachComponent<GalagaPlayer>();
        
    }
}