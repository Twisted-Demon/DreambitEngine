using Dreambit.Editor.Scenes;
using Dreambit.Tiled;
using ImGuiNET;

namespace Dreambit.Editor.UI.Panels;

internal sealed class TiledImportOptionsPanel(EditorDocumentContext documentContext)
    : EditorPanel(EditorPanelIds.TiledImportOptions, "Tiled Import Options")
{
    private string? _error;

    public override bool IsAvailable =>
        !documentContext.IsAsset &&
        !documentContext.IsBlueprint &&
        documentContext.Current?.TiledReference is not null;

    protected override void DrawContents()
    {
        var document = documentContext.Current;
        if (document?.TiledReference is null)
            return;

        var edited = (document.TiledReference.ImportOptions ?? new TiledImportOptions()).Clone();
        var pixelsPerUnit = edited.PixelsPerUnit;
        var baseDrawLayer = edited.BaseDrawLayer;
        var drawLayerStep = edited.DrawLayerStep;
        var worldDepth = edited.WorldDepth;
        var worldDepthStride = edited.WorldDepthDrawLayerStride;
        var renderBackgroundColor = edited.RenderMapBackgroundColor;
        var includeInvisibleLayers = edited.IncludeInvisibleLayers;
        var changed = false;
        string? mergeKey = null;

        if (ImGui.DragFloat("Pixels Per Unit", ref pixelsPerUnit, 0.1f, 0.001f, 100000f))
            (changed, mergeKey) = (true, "Tiled.PixelsPerUnit");
        if (ImGui.DragInt("Base Draw Layer", ref baseDrawLayer, 1f))
            (changed, mergeKey) = (true, "Tiled.BaseDrawLayer");
        if (ImGui.DragInt("Draw Layer Step", ref drawLayerStep, 1f, 1, 100000))
            (changed, mergeKey) = (true, "Tiled.DrawLayerStep");
        if (ImGui.DragInt("World Depth", ref worldDepth, 1f))
            (changed, mergeKey) = (true, "Tiled.WorldDepth");
        if (ImGui.DragInt("World Depth Stride", ref worldDepthStride, 1f, 1, int.MaxValue))
            (changed, mergeKey) = (true, "Tiled.WorldDepthStride");
        if (ImGui.Checkbox("Render Map Background Color", ref renderBackgroundColor))
            (changed, mergeKey) = (true, "Tiled.RenderBackgroundColor");
        if (ImGui.Checkbox("Include Invisible Layers", ref includeInvisibleLayers))
            (changed, mergeKey) = (true, "Tiled.IncludeInvisibleLayers");

        edited.PixelsPerUnit = pixelsPerUnit;
        edited.BaseDrawLayer = baseDrawLayer;
        edited.DrawLayerStep = drawLayerStep;
        edited.WorldDepth = worldDepth;
        edited.WorldDepthDrawLayerStride = worldDepthStride;
        edited.RenderMapBackgroundColor = renderBackgroundColor;
        edited.IncludeInvisibleLayers = includeInvisibleLayers;

        if (changed)
        {
            try
            {
                document.UpdateTiledImportOptions("Change Tiled Import Options", options =>
                {
                    options.PixelsPerUnit = edited.PixelsPerUnit;
                    options.BaseDrawLayer = edited.BaseDrawLayer;
                    options.DrawLayerStep = edited.DrawLayerStep;
                    options.WorldDepth = edited.WorldDepth;
                    options.WorldDepthDrawLayerStride = edited.WorldDepthDrawLayerStride;
                    options.RenderMapBackgroundColor = edited.RenderMapBackgroundColor;
                    options.IncludeInvisibleLayers = edited.IncludeInvisibleLayers;
                }, mergeKey);
                _error = null;
            }
            catch (Exception exception)
            {
                _error = exception.Message;
            }
        }

        ImGui.Spacing();
        if (ImGui.Button("Reimport Tiled Now", new System.Numerics.Vector2(-1f, 0f)))
        {
            try
            {
                document.ReimportTiled();
                _error = null;
            }
            catch (Exception exception)
            {
                _error = exception.Message;
            }
        }

        ImGui.TextDisabled("Live sync watches the .tmx map and referenced .tsx tilesets.");
        ImGui.TextDisabled("Object and image layers are intentionally ignored.");
        if (!string.IsNullOrWhiteSpace(_error))
            ImGui.TextColored(new System.Numerics.Vector4(0.96f, 0.34f, 0.36f, 1f), _error);
    }
}
