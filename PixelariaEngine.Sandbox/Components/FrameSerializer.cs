using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox.Components;

public class FrameSerializer : Component
{
    public SpriteSheetAnimation Animation;

    public override void OnCreated()
    {
        Animation = Resources.LoadAsset<SpriteSheetAnimation>("Animations/witch_run");
    }
}