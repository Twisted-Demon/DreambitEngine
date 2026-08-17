using Dreambit.Editor.Scenes;
using Dreambit.EditorApi;
using Dreambit.LDtk;

namespace Dreambit.Editor.Inspection;

internal sealed class LDtkImportInspector
{
    private string? _error;

    public void Draw(SceneDocument document)
    {
        var edited = (document.LDtkReference?.ImportOptions ?? new LDtkImportOptions()).Clone();
        var mergeKey = ImportOptionsEditorGui.Draw(edited);

        if (mergeKey is not null)
        {
            TryMutation(() => document.UpdateLDtkImportOptions(
                "Change LDtk Import Options",
                options =>
                {
                    options.PixelsPerUnit = edited.PixelsPerUnit;
                    options.BaseDrawLayer = edited.BaseDrawLayer;
                    options.DrawLayerStep = edited.DrawLayerStep;
                    options.WorldDepthDrawLayerStride = edited.WorldDepthDrawLayerStride;
                    options.RenderLevelBackgroundColor = edited.RenderLevelBackgroundColor;
                    options.RenderLevelBackgroundImage = edited.RenderLevelBackgroundImage;
                    options.IncludeInvisibleLayers = edited.IncludeInvisibleLayers;
                },
                mergeKey));
        }

        EditorGui.Space(EditorGuiSpacing.Section);
        if (EditorGui.FullWidthButton("LDtk.Reimport", "Reimport LDtk Now", primary: true))
            TryMutation(document.ReimportLDtk);

        EditorGui.MutedText("Live sync watches the .ldtk project and its external level files.", wrapped: true);
        DrawError();
    }

    private void TryMutation(Action mutation)
    {
        try
        {
            mutation();
            _error = null;
        }
        catch (Exception exception)
        {
            _error = exception.Message;
        }
    }

    private void DrawError()
    {
        if (!string.IsNullOrWhiteSpace(_error))
            EditorGui.Error(_error);
    }
}
