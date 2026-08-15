using Dreambit.Editor.Scenes;
using Dreambit.LDtk;
using ImGuiNET;

namespace Dreambit.Editor.UI.Panels;

internal sealed class LDtkImportOptionsPanel(EditorDocumentContext documentContext)
    : EditorPanel(EditorPanelIds.LDtkImportOptions, "LDtk Import Options")
{
    private string? _error;

    public override bool IsAvailable =>
        !documentContext.IsAsset &&
        !documentContext.IsBlueprint &&
        documentContext.Current?.LDtkReference is not null;

    protected override void DrawContents()
    {
        var document = documentContext.Current;
        if (document?.LDtkReference is null)
            return;

        var edited = (document.LDtkReference.ImportOptions ?? new LDtkImportOptions()).Clone();
        var pixelsPerUnit = edited.PixelsPerUnit;
        var baseDrawLayer = edited.BaseDrawLayer;
        var drawLayerStep = edited.DrawLayerStep;
        var worldDepthStride = edited.WorldDepthDrawLayerStride;
        var renderBackgroundColor = edited.RenderLevelBackgroundColor;
        var renderBackgroundImage = edited.RenderLevelBackgroundImage;
        var includeInvisibleLayers = edited.IncludeInvisibleLayers;
        var changed = false;
        string? mergeKey = null;

        if (ImGui.DragFloat("Pixels Per Unit", ref pixelsPerUnit, 0.1f, 0.001f, 100000f))
            (changed, mergeKey) = (true, "LDtk.PixelsPerUnit");
        if (ImGui.DragInt("Base Draw Layer", ref baseDrawLayer, 1f))
            (changed, mergeKey) = (true, "LDtk.BaseDrawLayer");
        if (ImGui.DragInt("Draw Layer Step", ref drawLayerStep, 1f, 1, 100000))
            (changed, mergeKey) = (true, "LDtk.DrawLayerStep");
        if (ImGui.DragInt("World Depth Stride", ref worldDepthStride, 1f, 1, int.MaxValue))
            (changed, mergeKey) = (true, "LDtk.WorldDepthStride");
        if (ImGui.Checkbox("Render Level Background Color", ref renderBackgroundColor))
            (changed, mergeKey) = (true, "LDtk.RenderBackgroundColor");
        if (ImGui.Checkbox("Render Level Background Image", ref renderBackgroundImage))
            (changed, mergeKey) = (true, "LDtk.RenderBackgroundImage");
        if (ImGui.Checkbox("Include Invisible Layers", ref includeInvisibleLayers))
            (changed, mergeKey) = (true, "LDtk.IncludeInvisibleLayers");

        edited.PixelsPerUnit = pixelsPerUnit;
        edited.BaseDrawLayer = baseDrawLayer;
        edited.DrawLayerStep = drawLayerStep;
        edited.WorldDepthDrawLayerStride = worldDepthStride;
        edited.RenderLevelBackgroundColor = renderBackgroundColor;
        edited.RenderLevelBackgroundImage = renderBackgroundImage;
        edited.IncludeInvisibleLayers = includeInvisibleLayers;

        if (changed)
        {
            try
            {
                document.UpdateLDtkImportOptions("Change LDtk Import Options", options =>
                {
                    options.PixelsPerUnit = edited.PixelsPerUnit;
                    options.BaseDrawLayer = edited.BaseDrawLayer;
                    options.DrawLayerStep = edited.DrawLayerStep;
                    options.WorldDepthDrawLayerStride = edited.WorldDepthDrawLayerStride;
                    options.RenderLevelBackgroundColor = edited.RenderLevelBackgroundColor;
                    options.RenderLevelBackgroundImage = edited.RenderLevelBackgroundImage;
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
        if (ImGui.Button("Reimport LDtk Now", new System.Numerics.Vector2(-1f, 0f)))
        {
            try
            {
                document.ReimportLDtk();
                _error = null;
            }
            catch (Exception exception)
            {
                _error = exception.Message;
            }
        }

        ImGui.TextDisabled("Live sync watches the .ldtk project and its external level files.");
        DrawError();
    }

    private void DrawError()
    {
        if (!string.IsNullOrWhiteSpace(_error))
            ImGui.TextColored(new System.Numerics.Vector4(0.96f, 0.34f, 0.36f, 1f), _error);
    }
}
