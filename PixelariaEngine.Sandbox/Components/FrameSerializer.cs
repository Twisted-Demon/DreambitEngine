using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox.Components;

public class FrameSerializer : Component
{
    public SpriteSheetAnimation Frames;

    public override void OnCreated()
    {

        
        Frames = Core.Instance.Content.Load<SpriteSheetAnimation>("Animations/witch_run");
    }
}