using System.Numerics;
using Dreambit.ECS;
using Dreambit.Editor.Assets;
using Dreambit.Editor.Persistence;
using Dreambit.Editor.Scenes;
using ImGuiNET;

namespace Dreambit.Editor.UI.Panels;

internal sealed class HierarchyPanel : EditorPanel
{
    private readonly AssetDatabase _assets;
    private readonly BlueprintSourceService _blueprintSources;
    private readonly EditorDocumentContext _documentContext;
    private readonly EditorDragDropService _dragDrop;
    private readonly EditorIconService _icons;
    private readonly EditorWorkspaceState _workspace;
    private string _blueprintSearch = string.Empty;
    private string? _error;
    private Guid[] _pendingDeleteIds = [];
    private string _rename = string.Empty;
    private Guid? _renameEntityId;
    private bool _requestBlueprintPicker;
    private bool _requestDeletePopup;
    private string _search = string.Empty;

    public HierarchyPanel(
        EditorDocumentContext documentContext,
        EditorDragDropService dragDrop,
        AssetDatabase assets,
        BlueprintSourceService blueprintSources,
        EditorWorkspaceState workspace,
        EditorIconService icons)
        : base(EditorPanelIds.Hierarchy, "Hierarchy")
    {
        _documentContext = documentContext;
        _dragDrop = dragDrop;
        _assets = assets;
        _blueprintSources = blueprintSources;
        _workspace = workspace;
        _icons = icons;
    }

    protected override void DrawContents()
    {
        var document = _documentContext.Current;
        if (document is not null &&
            ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
            _documentContext.Activate(document);
        if (_icons.Button("HierarchyCreate", "add", "Create entity"))
            ImGui.OpenPopup("Create Entity##Hierarchy");

        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint("##HierarchySearch", "Search entities", ref _search, 128);

        DrawCreateMenu(document);
        ImGui.Separator();

        if (document?.Scene is not { } scene)
        {
            ImGui.Spacing();
            ImGui.TextDisabled("No scene is open.");
            ImGui.TextWrapped("Create or open a .scene.json file from the File menu.");
            return;
        }

        DrawRootDropTarget(document);
        foreach (var root in scene.GetAllEntities()
                     .Where(entity => entity.Parent is null &&
                                       (!entity.IsEditorOnly || entity.IsImportedMapGenerated))
                     .ToArray())
            DrawEntity(document, root);

        DrawRenamePopup(document);
        DrawBlueprintPicker(document);
        DrawDeleteConfirmation(document);
        HandleKeyboard(document);
        _workspace.LastSelectedEntityIds = document.Selection.EntityIds.ToList();
        if (document.Selection.EntityIds.Count > 0)
            _workspace.LastSelectionKind = "entity";
        if (!string.IsNullOrWhiteSpace(_error))
        {
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(0.96f, 0.34f, 0.36f, 1f), _error);
        }
    }

    private static bool MatchesSearch(Entity entity, string search)
    {
        return string.IsNullOrWhiteSpace(search) ||
               entity.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
               entity.Children.Any(child => MatchesSearch(child, search));
    }

    private void DrawEntity(SceneDocument document, Entity entity)
{
    if (!MatchesSearch(entity, _search))
        return;

    var hasVisibleChildren =
        entity.Children.Any(child => MatchesSearch(child, _search));

    var flags =
        ImGuiTreeNodeFlags.SpanAvailWidth |
        ImGuiTreeNodeFlags.OpenOnArrow |
        ImGuiTreeNodeFlags.OpenOnDoubleClick;

    if (!hasVisibleChildren)
    {
        flags |=
            ImGuiTreeNodeFlags.Leaf |
            ImGuiTreeNodeFlags.NoTreePushOnOpen;
    }

    if (document.Selection.Contains(entity))
        flags |= ImGuiTreeNodeFlags.Selected;

    if (!entity.LocallyEnabled)
    {
        ImGui.PushStyleColor(
            ImGuiCol.Text,
            new Vector4(0.52f, 0.55f, 0.60f, 1f));
    }

    ImGui.SetNextItemOpen(
        _workspace.HierarchyExpandedEntityIds.Contains(entity.Id),
        ImGuiCond.Once);

    var boxedRoot =
        document.IsBlueprintInstanceRoot(entity);

    var displayName = entity.IsTiledGenerated
        ? $"[Tiled] {entity.Name}"
        : entity.IsLDtkGenerated
            ? $"[LDtk] {entity.Name}"
            : entity.Name;

    // The label is hidden because we draw the visible icon/name ourselves.
    // TreeNodeEx remains the actual interactive hierarchy item.
    var open = ImGui.TreeNodeEx(
        $"##Hierarchy.{entity.Id}",
        flags);

    DrawEntityLabel(
        boxedRoot
            ? "view_in_ar"
            : "account_tree",
        displayName);

    if (ImGui.IsItemToggledOpen())
    {
        if (open)
            _workspace.HierarchyExpandedEntityIds.Add(entity.Id);
        else
            _workspace.HierarchyExpandedEntityIds.Remove(entity.Id);
    }

    if (!entity.LocallyEnabled)
        ImGui.PopStyleColor();

    if (ImGui.IsItemClicked() &&
        !ImGui.IsItemToggledOpen())
    {
        document.Selection.Set(
            entity,
            ImGui.GetIO().KeyCtrl);
    }

    DrawDragSource(document, entity);
    DrawDropTarget(document, entity);
    DrawContextMenu(document, entity);

    if (open && hasVisibleChildren)
    {
        foreach (var child in entity.Children.ToArray())
            DrawEntity(document, child);

        ImGui.TreePop();
    }
}

    private void DrawEntityLabel(
        string icon,
        string text)
    {
        const float iconSize = 16f;
        const float iconSpacing = 4f;

        var itemMin = ImGui.GetItemRectMin();
        var itemMax = ImGui.GetItemRectMax();

        var labelX =
            itemMin.X +
            ImGui.GetTreeNodeToLabelSpacing();

        var rowHeight =
            itemMax.Y - itemMin.Y;

        var iconY =
            itemMin.Y +
            (rowHeight - iconSize) * 0.5f;

        var iconPosition =
            new Vector2(
                labelX,
                iconY);

        _icons.DrawAt(
            ImGui.GetWindowDrawList(),
            icon,
            iconPosition,
            new Vector2(iconSize, iconSize));

        var textSize =
            ImGui.CalcTextSize(text);

        var textPosition =
            new Vector2(
                labelX + iconSize + iconSpacing,
                itemMin.Y + (rowHeight - textSize.Y) * 0.5f);

        ImGui.GetWindowDrawList().AddText(
            textPosition,
            ImGui.GetColorU32(ImGuiCol.Text),
            text);
    }

    private void DrawCreateMenu(SceneDocument? document)
    {
        if (!ImGui.BeginPopup("Create Entity##Hierarchy"))
            return;
        ImGui.BeginDisabled(document is null);
        if (ImGui.MenuItem("Create Empty"))
            TryEdit(() => document!.CreateEmpty(
                "Entity",
                _documentContext.IsBlueprint ? _documentContext.Blueprints.Root : null));
        if (ImGui.MenuItem("Create From Blueprint"))
        {
            _blueprintSearch = string.Empty;
            _requestBlueprintPicker = true;
        }

        ImGui.EndDisabled();
        ImGui.EndPopup();
    }

    private void DrawContextMenu(SceneDocument document, Entity entity)
    {
        if (!ImGui.BeginPopupContextItem($"HierarchyContext##{entity.Id}"))
            return;

        var linked = document.TryGetBlueprintInstanceRoot(entity, out var instanceRoot, out var instance);
        var isInstanceRoot = linked && ReferenceEquals(entity, instanceRoot);
        var isBlueprintRoot = _documentContext.IsBlueprint &&
                              ReferenceEquals(entity, _documentContext.Blueprints.Root);
        if (entity.IsImportedMapGenerated)
        {
            ImGui.TextDisabled(entity.IsTiledGenerated
                ? "Generated from the linked Tiled map"
                : "Generated from the linked LDtk project");
            ImGui.Separator();
        }

        ImGui.BeginDisabled(linked || entity.IsImportedMapGenerated);
        if (ImGui.MenuItem("Create Child"))
            TryEdit(() => document.CreateEmpty("Entity", entity));
        ImGui.EndDisabled();
        ImGui.BeginDisabled(linked);
        if (ImGui.MenuItem("Rename", "F2"))
            RequestRename(entity);
        ImGui.EndDisabled();
        ImGui.BeginDisabled(isBlueprintRoot || entity.IsImportedMapGenerated || (linked && !isInstanceRoot));
        if (ImGui.MenuItem("Duplicate", "Ctrl+D"))
            TryEdit(() => document.Duplicate(entity));
        ImGui.EndDisabled();
        if (linked)
        {
            ImGui.Separator();
            ImGui.TextDisabled(instance.AssetName);
            if (ImGui.MenuItem("Unbox Blueprint Instance"))
                TryEdit(() => document.UnboxBlueprint(instanceRoot));
        }

        ImGui.Separator();
        ImGui.BeginDisabled(isBlueprintRoot || entity.IsImportedMapGenerated || (linked && !isInstanceRoot));
        if (ImGui.MenuItem("Delete", "Delete"))
            RequestDelete([entity]);
        ImGui.EndDisabled();
        ImGui.EndPopup();
    }

    private void DrawDragSource(SceneDocument document, Entity entity)
    {
        if (_documentContext.IsBlueprint && ReferenceEquals(entity, _documentContext.Blueprints.Root))
            return;
        if (entity.IsImportedMapGenerated)
            return;
        if (document.TryGetBlueprintInstanceRoot(entity, out var instanceRoot, out _) &&
            !ReferenceEquals(entity, instanceRoot))
            return;
        if (!ImGui.BeginDragDropSource())
            return;
        _dragDrop.SetHierarchyEntity(entity.Id);
        ImGui.SetDragDropPayload(EditorDragDropService.HierarchyEntityPayloadType, IntPtr.Zero, 0);
        ImGui.TextUnformatted(entity.Name);
        ImGui.EndDragDropSource();
    }

    private unsafe void DrawDropTarget(SceneDocument document, Entity parent)
    {
        if (parent.IsImportedMapGenerated)
            return;
        if (document.TryGetBlueprintInstanceRoot(parent, out _, out _))
            return;
        if (!ImGui.BeginDragDropTarget())
            return;
        var payload = ImGui.AcceptDragDropPayload(EditorDragDropService.HierarchyEntityPayloadType);
        if (payload.NativePtr != null && _dragDrop.HierarchyEntityId is { } id &&
            document.Scene?.FindEntity(id) is { } entity)
        {
            TryReparent(document, entity, parent);
            _dragDrop.ClearHierarchyEntity();
        }

        ImGui.EndDragDropTarget();
    }

    private unsafe void DrawRootDropTarget(SceneDocument document)
    {
        ImGui.InvisibleButton("##HierarchyRootDrop", new Vector2(-1f, 5f));
        if (!ImGui.BeginDragDropTarget())
            return;
        var payload = ImGui.AcceptDragDropPayload(EditorDragDropService.HierarchyEntityPayloadType);
        if (payload.NativePtr != null && _dragDrop.HierarchyEntityId is { } id &&
            document.Scene?.FindEntity(id) is { } entity)
        {
            TryReparent(
                document,
                entity,
                _documentContext.IsBlueprint ? _documentContext.Blueprints.Root : null);
            _dragDrop.ClearHierarchyEntity();
        }

        ImGui.EndDragDropTarget();
    }

    private void TryReparent(SceneDocument document, Entity entity, Entity? parent)
    {
        TryEdit(() => document.Reparent(entity, parent));
    }

    private void RequestRename(Entity entity)
    {
        _renameEntityId = entity.Id;
        _rename = entity.Name;
        ImGui.OpenPopup("Rename Entity##Hierarchy");
    }

    private void DrawRenamePopup(SceneDocument document)
    {
        if (!ImGui.BeginPopupModal("Rename Entity##Hierarchy", ImGuiWindowFlags.AlwaysAutoResize))
            return;
        ImGui.SetNextItemWidth(320f);
        var submit = ImGui.InputText("Name", ref _rename, 256, ImGuiInputTextFlags.EnterReturnsTrue);
        var entity = _renameEntityId is { } id ? document.Scene?.FindEntity(id) : null;
        if ((submit || ImGui.Button("Rename")) && entity is not null && !string.IsNullOrWhiteSpace(_rename))
            if (TryEdit(() => document.Rename(entity, _rename)))
                ImGui.CloseCurrentPopup();
        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private void DrawBlueprintPicker(SceneDocument document)
    {
        if (_requestBlueprintPicker)
        {
            ImGui.OpenPopup("Create From Blueprint##Hierarchy");
            _requestBlueprintPicker = false;
        }

        if (!ImGui.BeginPopupModal(
                "Create From Blueprint##Hierarchy",
                ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.SetNextItemWidth(420f);
        ImGui.InputTextWithHint("##BlueprintSearch", "Search Blueprints", ref _blueprintSearch, 256);
        ImGui.BeginChild("##BlueprintResults", new Vector2(420f, 280f), ImGuiChildFlags.Borders);
        var blueprints = _assets.GetSnapshot().Assets
            .Where(asset => asset.Kind == AssetKind.Blueprint &&
                            (string.IsNullOrWhiteSpace(_blueprintSearch) ||
                             asset.RelativePath.Contains(_blueprintSearch, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        if (blueprints.Length == 0)
            ImGui.TextDisabled("No matching Entity Blueprints.");
        foreach (var blueprint in blueprints)
        {
            if (!ImGui.Selectable(blueprint.RelativePath))
                continue;
            try
            {
                using var source = _blueprintSources.Load(blueprint);
                document.InstantiateBlueprint(
                    source,
                    parent: _documentContext.IsBlueprint ? _documentContext.Blueprints.Root : null);
                _error = null;
                ImGui.CloseCurrentPopup();
            }
            catch (Exception exception)
            {
                _error = $"Could not instantiate Blueprint. {exception.Message}";
            }
        }

        ImGui.EndChild();
        if (ImGui.Button("Cancel", new Vector2(90f, 0f)))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private void RequestDelete(IEnumerable<Entity> entities)
    {
        _pendingDeleteIds = entities.Select(entity => entity.Id).Distinct().ToArray();
        _requestDeletePopup = _pendingDeleteIds.Length > 0;
    }

    private void DrawDeleteConfirmation(SceneDocument document)
    {
        if (_requestDeletePopup)
        {
            ImGui.OpenPopup("Delete Entities##Hierarchy");
            _requestDeletePopup = false;
        }

        if (!ImGui.BeginPopupModal("Delete Entities##Hierarchy", ImGuiWindowFlags.AlwaysAutoResize))
            return;

        var entities = _pendingDeleteIds
            .Select(id => document.Scene?.FindEntity(id))
            .OfType<Entity>()
            .ToArray();
        ImGui.TextWrapped(entities.Length == 1
            ? $"Delete '{entities[0].Name}' and all of its children?"
            : $"Delete {entities.Length} selected entities and all of their children?");
        ImGui.TextDisabled("This action can be undone with Ctrl+Z.");
        ImGui.Spacing();
        if (ImGui.Button("Delete", new Vector2(90f, 0f)))
            if (TryEdit(() => document.Delete(entities)))
            {
                _pendingDeleteIds = [];
                ImGui.CloseCurrentPopup();
            }

        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(90f, 0f)))
        {
            _pendingDeleteIds = [];
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private void HandleKeyboard(SceneDocument document)
    {
        if (!ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
            return;
        var selected = document.Selection.Resolve(document.Scene);
        if (selected.Count == 0)
            return;
        var hasLockedBlueprintChild = selected.Any(entity =>
            document.TryGetBlueprintInstanceRoot(entity, out var instanceRoot, out _) &&
            !ReferenceEquals(entity, instanceRoot));
        var hasGeneratedMapEntity = selected.Any(entity => entity.IsImportedMapGenerated);
        var includesBlueprintRoot = _documentContext.IsBlueprint &&
                                    selected.Any(entity => ReferenceEquals(entity, _documentContext.Blueprints.Root));
        if (ImGui.IsKeyPressed(ImGuiKey.Delete) && !includesBlueprintRoot && !hasLockedBlueprintChild &&
            !hasGeneratedMapEntity)
            RequestDelete(selected);
        if (ImGui.IsKeyPressed(ImGuiKey.F2) && selected.Count == 1 &&
            !document.TryGetBlueprintInstanceRoot(selected[0], out _, out _))
            RequestRename(selected[0]);
        if (ImGui.GetIO().KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.D) && selected.Count == 1 &&
            !includesBlueprintRoot &&
            !hasLockedBlueprintChild && !hasGeneratedMapEntity)
            TryEdit(() => document.Duplicate(selected[0]));
    }

    private bool TryEdit(Action edit)
    {
        try
        {
            edit();
            _error = null;
            return true;
        }
        catch (Exception exception)
        {
            _error = exception.Message;
            return false;
        }
    }
}
