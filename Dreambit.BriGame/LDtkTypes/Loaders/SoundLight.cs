using Dreambit.BriGame.Components.InternalDev;
using Dreambit.ECS;
using Dreambit.ECS.Audio;
using LDtk;

namespace Dreambit.BriGame;

public partial class SoundLight : LDtkEntity<SoundLight>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this, tags: ["light"]);

        var pointLight = entity.AttachComponent<PointLight2D>();

        pointLight.Intensity = Intensity;
        pointLight.Radius = Radius;
        pointLight.Color = Color;

        var soundComponent = entity.AttachComponent<SoundEmitter2d>();
        soundComponent.SoundEffectPath = "Sounds/UI/Classic UI SFX - Chords #19";

        entity.AttachComponent<PlaySoundLightTest>();
    }
}