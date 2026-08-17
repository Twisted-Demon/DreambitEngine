using Dreambit.Editor.Inspection;
using Dreambit.Editor.Scenes;

namespace Dreambit.Editor.UI.Panels;

internal sealed class LDtkImportOptionsPanel(EditorDocumentContext documentContext)
    : EditorPanel(EditorPanelIds.LDtkImportOptions, "LDtk Import Options")
{
    private readonly LDtkImportInspector _inspector = new();

    public override bool IsAvailable =>
        !documentContext.IsAsset &&
        !documentContext.IsBlueprint &&
        documentContext.Current?.LDtkReference is not null;

    protected override void DrawContents()
    {
        var document = documentContext.Current;
        if (document?.LDtkReference is null)
            return;
        _inspector.Draw(document);
    }
}
