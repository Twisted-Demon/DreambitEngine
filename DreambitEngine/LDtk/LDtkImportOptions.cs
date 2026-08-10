using System;

namespace Dreambit.LDtk;

public sealed class LDtkImportOptions
{
    /// <summary>Number of LDtk source pixels represented by one Dreambit world unit.</summary>
    public float PixelsPerUnit { get; init; } = 1f;

    /// <summary>Lowest draw layer used for a world-depth slice.</summary>
    public int BaseDrawLayer { get; init; }

    /// <summary>Distance between adjacent LDtk visual layers.</summary>
    public int DrawLayerStep { get; init; } = 1;

    /// <summary>Draw-layer distance between LDtk worldDepth values.</summary>
    public int WorldDepthDrawLayerStride { get; init; } = 1000;

    public bool RenderLevelBackgroundColor { get; init; } = true;
    public bool RenderLevelBackgroundImage { get; init; } = true;
    public bool IncludeInvisibleLayers { get; init; }

    internal void Validate()
    {
        if (!float.IsFinite(PixelsPerUnit) || PixelsPerUnit <= 0f)
            throw new ArgumentOutOfRangeException(nameof(PixelsPerUnit), "PixelsPerUnit must be positive and finite.");
        if (DrawLayerStep <= 0)
            throw new ArgumentOutOfRangeException(nameof(DrawLayerStep), "DrawLayerStep must be positive.");
        if (WorldDepthDrawLayerStride <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(WorldDepthDrawLayerStride),
                "WorldDepthDrawLayerStride must be positive.");
    }
}
