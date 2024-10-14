using PixelariaEngine.ECS;
using PixelariaEngine.Graphics;

namespace PixelariaEngine.Sandbox;

public class TransitionSceneTest : Scene
{
    private Entity _bgEntity;
    private Entity _playerEntity;

    protected override void OnInitialize()
    {
        _bgEntity = Entity.Create("bg");
        var bgSprite = _bgEntity.AttachComponent<SpriteDrawer>();
        bgSprite.SpriteSheetPath = "SpriteSheets/SpaceBackground";
        bgSprite.PivotType = PivotType.BottomCenter;

        _playerEntity = Entity.Create("player");
        _playerEntity.AttachComponent<SandboxController>();

        MainCamera.IsFollowing = true;
        MainCamera.TransformToFollow = _playerEntity.Transform;
        
    }

    protected override void OnUpdate()
    {
        //Scene.SetNextScene(new TransitionSceneTest().AddRenderer<DefaultRenderer>());
    }
}