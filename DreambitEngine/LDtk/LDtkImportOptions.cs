using System;
using Newtonsoft.Json;

namespace Dreambit.LDtk;

public sealed class LDtkImportOptions
{
    /// <summary>Number of LDtk source pixels represented by one Dreambit world unit.</summary>
    [JsonProperty("pixels_per_unit")]
    public float PixelsPerUnit { get; set; } = 1f;

    /// <summary>Lowest draw layer used for a world-depth slice.</summary>
    [JsonProperty("base_draw_layer")]
    public int BaseDrawLayer { get; set; }

    /// <summary>Distance between adjacent LDtk visual layers.</summary>
    [JsonProperty("draw_layer_step")]
    public int DrawLayerStep { get; set; } = 1;

    /// <summary>Draw-layer distance between LDtk worldDepth values.</summary>
    [JsonProperty("world_depth_draw_layer_stride")]
    public int WorldDepthDrawLayerStride { get; set; } = 1000;

    [JsonProperty("render_level_background_color")]
    public bool RenderLevelBackgroundColor { get; set; } = true;

    [JsonProperty("render_level_background_image")]
    public bool RenderLevelBackgroundImage { get; set; } = true;

    [JsonProperty("include_invisible_layers")]
    public bool IncludeInvisibleLayers { get; set; }

    public LDtkImportOptions Clone() => new()
    {
        PixelsPerUnit = PixelsPerUnit,
        BaseDrawLayer = BaseDrawLayer,
        DrawLayerStep = DrawLayerStep,
        WorldDepthDrawLayerStride = WorldDepthDrawLayerStride,
        RenderLevelBackgroundColor = RenderLevelBackgroundColor,
        RenderLevelBackgroundImage = RenderLevelBackgroundImage,
        IncludeInvisibleLayers = IncludeInvisibleLayers
    };

    public void Validate()
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
