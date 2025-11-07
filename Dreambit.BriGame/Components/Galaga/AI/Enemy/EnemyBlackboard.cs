namespace Dreambit.BriGame.Components.Galaga;

public class EnemyBlackboard : Blackboard
{
    public int DamageTaken = 0;
    public EnemyType EnemyType = EnemyType.Bee;
    public float FlySpeed = 80.0f;
    public GalagaPlayer GalagaPlayer;

    public SpriteSheet BeeSpriteSheet;
    public SpriteSheet ButterflySpriteSheet;
    public SpriteSheet BossHealthySpriteSheet;
    public SpriteSheet BossHurtSpriteSheet;
    public SpriteSheet ExplosionSpriteSheet;

    public FormationSlot FormationSlot;

    public bool ArrivedToFlyInPoint = false;
}