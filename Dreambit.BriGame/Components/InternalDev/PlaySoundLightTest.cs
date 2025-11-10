using Dreambit.ECS;
using Dreambit.ECS.Audio;
using Microsoft.Xna.Framework.Input;

namespace Dreambit.BriGame.Components.InternalDev;

public class PlaySoundLightTest : Component
{
    private SoundEmitter2d _soundEmitter;

    public override void OnAddedToEntity()
    {
        _soundEmitter = Entity.GetComponent<SoundEmitter2d>();
    }

    public override void OnUpdate()
    {
        if (Input.IsKeyPressed(Keys.O))
            _soundEmitter.Play3D();
    }
}