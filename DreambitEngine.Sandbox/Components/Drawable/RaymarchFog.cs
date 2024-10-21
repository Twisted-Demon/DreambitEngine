using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.Sandbox;

public class RaymarchFog : DrawableComponent<RaymarchFog>
{
    public int Width;
    public int Height;
    public PivotType PivotType { get; set; } = PivotType.Center;
    public Vector2 Pivot { get; set; }

    public override Rectangle Bounds => GetBounds();

    public override void OnCreated()
    {
        Effect = Resources.LoadAsset<Effect>("Effects/raymarchFog");
        DrawLayer = 5;
    }

    public override void OnPreDraw()
    {
        Effect.Parameters["fogDensity"].SetValue(.5f);
        Effect.Parameters["fogStart"].SetValue(0);
        Effect.Parameters["fogEnd"].SetValue(Window.Height);
        Effect.Parameters["fogColor"].SetValue(new Vector4(1f, 1f, 1f, 1f));
    }

    public override void OnDraw()
    {
        Core.SpriteBatch.DrawFilledRectangle(Bounds, Color.DarkGray);
    }


    private Rectangle GetBounds()
    {
        var pivotToUse = Transform.WorldPosToVec2;
        
        switch (PivotType)
        {
            case PivotType.Custom:
                pivotToUse -= Pivot;
                break;
            default:
                var pivotOffset = PivotHelper.GetRelativePivot(PivotType);
                pivotToUse -= new Vector2(pivotOffset.X * Width, pivotOffset.Y * Height);
                break;
        }
        
        var bounds = new Rectangle(
            (int)pivotToUse.X, 
            (int)pivotToUse.Y, 
            Width, 
            Height);

        return bounds;
    }
}