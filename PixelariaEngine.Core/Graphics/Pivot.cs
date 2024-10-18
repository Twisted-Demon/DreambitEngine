using Microsoft.Xna.Framework;

namespace PixelariaEngine;

public static class PivotHelper
{
    public static Vector2 GetRelativePivot(PivotType pivotTypeType)
    {
        return pivotTypeType switch
        {
            PivotType.TopLeft => new Vector2(0f, 0f),
            PivotType.TopCenter => new Vector2(0.5f, 0),
            PivotType.TopRight => new Vector2(1f, 0f),
            PivotType.CenterLeft => new Vector2(0f, 0.5f),
            PivotType.Center => new Vector2(0.5f, 0.5f),
            PivotType.CenterRight => new Vector2(1f, 0.5f),
            PivotType.BottomLeft => new Vector2(0f, 1f),
            PivotType.BottomCenter => new Vector2(0.5f, 1f),
            PivotType.BottomRight => new Vector2(1f, 1f),
            PivotType.Custom => new Vector2(0.5f, 0.5f),
            _ => new Vector2(0.5f, 0.5f)
        };
    }
}