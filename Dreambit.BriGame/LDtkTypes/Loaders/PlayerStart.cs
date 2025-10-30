using Dreambit.BriGame.Components;
using Dreambit.ECS;
using Dreambit.ECS.Audio;
using LDtk;
using Microsoft.Xna.Framework;

namespace Dreambit.BriGame;

public partial class PlayerStart : LDtkEntity<PlayerStart>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this, tags: ["player"]);
        entity.Name = "player";
        Scene.MainCamera.ForcePosition(entity.Transform.WorldPosition);
        Scene.DebugMode = false;

        entity.AttachComponent<PlayerController>();

        var light = Entity.CreateChildOf(entity, "Player Light");

        var lightComponent = light.AttachComponent<PointLight2D>();
        lightComponent.Color = Color.White;
        lightComponent.Radius = 75f;
        lightComponent.Intensity = 0.5f;

        entity.AttachComponent<SoundListener2d>();
    }
}