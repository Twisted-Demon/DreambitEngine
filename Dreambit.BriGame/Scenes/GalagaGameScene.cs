using Dreambit.BriGame.Components.Galaga;
using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.BriGame.Scenes.Galaga;

public class GalagaGameScene: Scene<GalagaGameScene>
{
    public GalagaPlayer Player;
    
    protected override void OnInitialize()
    {
        SetUpCamera();
        SetUpLighting();

        FormationManager.Spawn();
        Player = GalagaPlayer.SpawnPlayer();
    }

    private void SetUpCamera()
    {
        MainCamera.SetTargetVerticalResolution(GalagaConstants.GameHeight);
        // make camera look at center, 0,0 should be top left
        MainCamera.ForcePosition(
            new Vector2((float)GalagaConstants.GameWidth / 2, (float)GalagaConstants.GameHeight / 2).ToVector3());
    }

    private void SetUpLighting()
    {
        var ambientLight = CreateEntity("ambient_light")
            .AttachComponent<AmbientLight2D>();
        
        ambientLight.Color = Color.White;
        
        BackgroundColor = Color.Black;
        
        RenderingOptions.SamplerState = SamplerState.PointClamp;
    }
}