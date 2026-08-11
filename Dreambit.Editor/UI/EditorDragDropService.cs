using Dreambit;
using Dreambit.Editor.Assets;

namespace Dreambit.Editor.UI;

internal sealed class EditorDragDropService
{
    public const string ProjectItemPayloadType = "DREAMBIT_PROJECT_ITEM";
    public const string HierarchyEntityPayloadType = "DREAMBIT_HIERARCHY_ENTITY";

    public ProjectItemDragPayload? ProjectItem { get; private set; }
    public Guid? HierarchyEntityId { get; private set; }

    public void SetProjectItem(ProjectItemDragPayload payload) => ProjectItem = payload;

    public void ClearProjectItem() => ProjectItem = null;

    public void SetHierarchyEntity(Guid entityId) => HierarchyEntityId = entityId;

    public void ClearHierarchyEntity() => HierarchyEntityId = null;
}

internal sealed record ProjectItemDragPayload(
    string RelativePath,
    bool IsFolder,
    AssetId AssetId,
    AssetKind Kind,
    string? TypeName);
