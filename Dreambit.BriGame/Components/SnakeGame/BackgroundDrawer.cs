using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit.BriGame.Components;

public class BackgroundDrawer : DrawableComponent
{
    public override Rectangle Bounds => new(0, 0, 800, 800);

    public override void OnDraw()
    {
        var rows = 21;
        var columns = 21;

        var width = 40;
        var height = 40;

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                var index = y * columns + x;
                
                var rect = new Rectangle(x * width,  y * height, width, height);

                var color = new Color(38, 150, 65);
                if(index % 2 == 0)
                    color = new Color(44, 164, 72);

                Core.SpriteBatch.DrawFilledRectangle(rect, color);
            }
        }
    }
}