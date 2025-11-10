using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.Examples.SpaceGame;

public class SpaceGameScene : Scene<SpaceGameScene>
{
    protected override void OnInitialize()
    {
        InitializeSettings();
        
        RenderingOptions.SamplerState = SamplerState.PointClamp;
        RenderingOptions.UISamplerState = SamplerState.PointClamp;
        
        AmbientLight.Intensity = 1.0f;
        AmbientLight.Color = Color.White;
        
        SpawnPlayer();
        
        var alanBp = Resources.LoadAsset<EntityBlueprint>("SpaceGame/Blueprints/alan");
        
        CreateEntity(alanBp, createAt: new Vector3(90, 40, 0));
        CreateEntity(alanBp, createAt: new Vector3(45, 40, 0));
        CreateEntity(alanBp, createAt: new Vector3(135, 40, 0));
        
    }

    private void InitializeSettings()
    {
        // Backbuffer in real pixels:
        Window.SetSize(
            SpaceGameSettings.WindowWidth,
            SpaceGameSettings.WindowHeight);

        // WORLD units
        MainCamera.PixelsPerUnit = 1;
        MainCamera.SetTargetVerticalResolution(SpaceGameSettings.GameHeight); 
        MainCamera.ForcePosition(new Vector3(
            SpaceGameSettings.GameWidth * 0.5f,
            SpaceGameSettings.GameHeight * 0.5f,
            0)); 


        UICamera.PixelsPerUnit = 1;
        UICamera.SetTargetVerticalResolution(SpaceGameSettings.TargetUIHeight); // e.g., 720
    }
    

    private void SpawnPlayer()
    {
        var spawn = new Vector3(SpaceGameSettings.GameWidth * 0.5f,  SpaceGameSettings.GameHeight - 12, 0);
        var playerBp = Resources.LoadAsset<EntityBlueprint>("SpaceGame/Blueprints/player_ship");

        CreateEntity(playerBp, createAt: spawn);
    }
}