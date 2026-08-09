using Dreambit;
using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace DreambitGame;

public sealed class MainScene : Scene
{
    protected override void OnInitialize()
    {
        var entity = Entity.Create("Welcome Rectangle");
        var rectangle = entity.AttachComponent<RectDrawer>();

        rectangle.Width = 64;
        rectangle.Height = 64;
        rectangle.Color = Color.White;

        MainCamera.ForcePosition(entity.Transform.WorldPosition);
    }
}
