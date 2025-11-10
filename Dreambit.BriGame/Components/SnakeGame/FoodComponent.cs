using Dreambit.ECS;
using Dreambit.ECS.Audio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Dreambit.BriGame.Components;

[Require(typeof(SpriteDrawer), typeof(SoundEffectEmitter))]
public class FoodComponent : Component
{
    public SpriteDrawer SpriteDrawer;
    public SpriteSheet FoodSpriteSheet;
    public SoundEffectEmitter SoundEmitter;

    public SoundCue EatSoundCue;
    
    public Point CellPosition { get; set; }

    public override void OnCreated()
    {
        EatSoundCue = Resources.LoadAsset<SoundCue>("Sounds/SnakeGame/Cues/eat_food");
    }

    public override void OnAddedToEntity()
    {
        SpriteDrawer = Entity.GetComponent<SpriteDrawer>();
        SoundEmitter = Entity.GetComponent<SoundEffectEmitter>();
        FoodSpriteSheet = SpriteSheet.Create(1, 1, "Textures/SnakeGame/apple");
        SpriteDrawer.SpriteSheet = FoodSpriteSheet;
        
        SoundEmitter.SoundCue = EatSoundCue;
    }

    public override void OnDestroyed()
    {
        SoundEmitter.Play(EatSoundCue);
    }
}