using System.Numerics;
using Dreambit.ECS;
using Dreambit.Editor.Assets;
using Dreambit.Editor.Persistence;
using Dreambit.Editor.Scenes;
using Dreambit.Editor.UI;
using ImGuiNET;

namespace Dreambit.Editor.UI.Panels;

internal sealed class HierarchyPanel : EditorPanel
{
    private readonly SceneDocumentService _documents;
    private readonly SelectionService _selection;
    private readonly EditorDragDropService _dragDrop;
    private readonly AssetDatabase _assets;
    private readonly EditorWorkspaceState _workspace;
    private string _search = string.Empty;
    private string _blueprintSearch = string.Empty;
    private string _rename = string.Empty;
    private Guid? _renameEntityId;
    private Guid[] _pendingDeleteIds = [];
    private bool _requestDeletePopup;
    private bool _requestBlueprintPicker;
    private string? _error;

    public HierarchyPanel(
        SceneDocumentService documents,
        SelectionService selection,
        EditorDragDropService dragDrop,
        AssetDatabase assets,
        EditorWorkspaceState workspace)
        : base(EditorPanelIds.Hierarchy, "Hierarchy")
    {
        _documents = documents;
        _selection = selection;
        _dragDrop = dragDrop;
        _assets = assets;
        _workspace = workspace;
    }

    protected override void DrawContents()
    {
        var document = _documents.Current;
        if (ImGui.Button("+", new Vector2(28f, 0f)))
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
                     .Where(entity => entity.Parent is null && !entity.IsEditorOnly)
                     .ToArray())
            DrawEntity(document, root);

        DrawRenamePopup(document);
        DrawBlueprintPicker(document);
        DrawDeleteConfirmation(document);
        HandleKeyboard(document);
        _workspace.LastSelectedEntityIds = _selection.EntityIds.ToList();
        if (_selection.EntityIds.Count > 0)
            _workspace.LastSelectionKind = "entity";
        if (!string.IsNullOrWhiteSpace(_error))
        {
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(0.96f, 0.34f, 0.36f, 1f), _error);
        }
    }

    private static bool MatchesSearch(Entity entity, string search) =>
        string.IsNullOrWhiteSpace(search) ||
        entity.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
        entity.Children.Any(child => MatchesSearch(child, search));

    private void DrawEntity(SceneDocument document, Entity entity)
    {
        if (!MatchesSearch(entity, _search))
            return;

        var hasVisibleChildren = entity.Children.Any(child => MatchesSearch(child, _search));
        var flags = ImGuiTreeNodeFlags.SpanAvailWidth |
                    ImGuiTreeNodeFlags.OpenOnArrow |
                    ImGuiTreeNodeFlags.OpenOnDoubleClick;
        if (!hasVisibleChildren)
            flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
        if (_selection.Contains(entity))
            flags |= ImGuiTreeNodeFlags.Selected;
        if (!entity.LocallyEnabled)
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.52f, 0.55f, 0.60f, 1f));

        ImGui.SetNextItemOpen(
            _workspace.HierarchyExpandedEntityIds.Contains(entity.Id),
            ImGuiCond.Once);
        var open = ImGui.TreeNodeEx($"{entity.Name}##Hierarchy.{entity.Id}", flags);
        if (ImGui.IsItemToggledOpen())
        {
            if (open)
                _workspace.HierarchyExpandedEntityIds.Add(entity.Id);
            else
                _workspace.HierarchyExpandedEntityIds.Remove(entity.Id);
        }
        if (!entity.LocallyEnabled)
            ImGui.PopStyleColor();
        if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen())
            _selection.Set(entity, ImGui.GetIO().KeyCtrl);

        DrawDragSource(entity);
        DrawDropTarget(document, entity);
        DrawContextMenu(document, entity);

        if (open && hasVisibleChildren)
        {
            foreach (var child in entity.Children.ToArray())
                DrawEntity(document, child);
            ImGui.TreePop();
        }
    }

    private void DrawCreateMenu(SceneDocument? document)
    {
        if (!ImGui.BeginPopup("Create Entity##Hierarchy"))
            return;
        ImGui.BeginDisabled(document is null);
        if (ImGui.MenuItem("Create Empty"))
            document!.CreateEmpty();
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

        if (ImGui.MenuItem("Create Child"))
            document.CreateEmpty("Entity", entity);
        if (ImGui.MenuItem("Rename", "F2"))
            RequestRename(entity);
        if (ImGui.MenuItem("Duplicate", "Ctrl+D"))
            document.Duplicate(entity);
        ImGui.Separator();
        if (ImGui.MenuItem("Delete", "Delete"))
            RequestDelete([entity]);
        ImGui.EndPopup();
    }

    private void DrawDragSource(Entity entity)
    {
        if (!ImGui.BeginDragDropSource())
            return;
        _dragDrop.SetHierarchyEntity(entity.Id);
        ImGui.SetDragDropPayload(EditorDragDropService.HierarchyEntityPayloadType, IntPtr.Zero, 0);
        ImGui.TextUnformatted(entity.Name);
        ImGui.EndDragDropSource();
    }

    private unsafe void DrawDropTarget(SceneDocument document, Entity parent)
    {
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
            TryReparent(document, entity, null);
            _dragDrop.ClearHierarchyEntity();
        }
        ImGui.EndDragDropTarget();
    }

    private void TryReparent(SceneDocument document, Entity entity, Entity? parent)
    {
        try
        {
            document.Reparent(entity, parent);
            _error = null;
        }
        catch (Exception exception)
        {
            _error = exception.Message;
        }
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
        {
            document.Rename(entity, _rename);
            ImGui.CloseCurrentPopup();
        }
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
                var path = Path.Combine(
                    _assets.ContentRoot,
                    blueprint.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                var source = DreambitJson.Deserialize<EntityBlueprint>(File.ReadAllText(path))
                             ?? throw new InvalidDataException("Blueprint file is empty.");
                document.InstantiateBlueprint(source);
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
        {
            document.Delete(entities);
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
        var selected = _selection.Resolve(document.Scene);
        if (selected.Count == 0)
            return;
        if (ImGui.IsKeyPressed(ImGuiKey.Delete))
            RequestDelete(selected);
        if (ImGui.IsKeyPressed(ImGuiKey.F2) && selected.Count == 1)
            RequestRename(selected[0]);
        if (ImGui.GetIO().KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.D) && selected.Count == 1)
            document.Duplicate(selected[0]);
    }
}
