using Dreambit.BriGame.Components;
using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.BriGame.Scenes;

public class SnakeGameScene : Scene
{
    protected override void OnInitialize()
    {
        BackgroundColor = Color.Green;
        RenderingOptions.SamplerState = SamplerState.AnisotropicClamp;
        
        MainCamera.SetTargetVerticalResolution(840);
        MainCamera.ForcePosition(new Vector3((float)840 / 2, (float)840 / 2, 0));
        
        var ambientLightEntity = CreateEntity("ambientLight");
        var ambientLight = ambientLightEntity.AttachComponent<AmbientLight2D>();
        ambientLight.Color = Color.White;
        
        var backgroundEntity = CreateEntity("background");
        backgroundEntity.AttachComponent<BackgroundDrawer>();
        
        var gridManager = CreateEntity("gridManager");
        gridManager.AttachComponent<GridManager>();
    }
}