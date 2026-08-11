using Dreambit;
using Dreambit.Editor.Assets;

namespace Dreambit.Editor.UI;

internal sealed class EditorDragDropService
{
    public const string ProjectItemPayloadType = "DREAMBIT_PROJECT_ITEM";

    public ProjectItemDragPayload? ProjectItem { get; private set; }

    public void SetProjectItem(ProjectItemDragPayload payload) => ProjectItem = payload;

    public void ClearProjectItem() => ProjectItem = null;
}

internal sealed record ProjectItemDragPayload(
    string RelativePath,
    bool IsFolder,
    AssetId AssetId,
    AssetKind Kind,
    string? TypeName);
