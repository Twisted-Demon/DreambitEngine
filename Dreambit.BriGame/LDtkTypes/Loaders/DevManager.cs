using System.Runtime.CompilerServices;
using Dreambit.BriGame.Components.InternalDev;
using Dreambit.ECS;
using LDtk;
using Microsoft.Xna.Framework;

namespace Dreambit.BriGame;

public partial class DevManager : LDtkEntity<DevManager>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this, "dev_manager", ["dev"]);
        entity.AttachComponent<DebugToggleComponent>();

        entity.Transform.Position = Vector3.Zero;

        var canvas = entity.AttachComponent<Canvas>();


        var button = canvas.CreateUIElement<UIButton>();
        button.Texture.TexturePath = "Textures/Ui/ui_button";
        button.Texture.CanScale = false;

        button.UIText.FontPath = "Fonts/monogram";
        button.UIText.Text = "button Text";
        button.UIText.HAlignment = HorizontalAlignment.Center;

        button.OnClick += () =>
        {
            ILogger logger = new Logger<UIButton>();
            logger.Info("Clicked");
        };
    }
}