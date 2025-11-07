using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.BriGame.Scenes;

public class BriWorldScene : LDtkScene
{
    protected override void OnInitialize()
    {
        MainCamera.PixelsPerUnit = 4;
        RenderingOptions.SamplerState = SamplerState.PointClamp;
    }
}