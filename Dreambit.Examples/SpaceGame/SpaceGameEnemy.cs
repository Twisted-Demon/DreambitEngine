using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit.Examples.SpaceGame;

[Require(typeof(SpriteAnimator), typeof(Mover))]
public class SpaceGameEnemy : Component<SpaceGameEnemy>
{
    [FromRequired]
    private SpriteAnimator Animator { get; set; }
    
    [FromRequired]
    private Mover Mover { get; set; }
    
    public override void OnCreated()
    {
        Animator.RegisterEvent("alan_shoot", EventAction);
    }

    private void EventAction()
    {
        SpaceGameProjectile.CreateAlanProjectile(Entity);
    }
}