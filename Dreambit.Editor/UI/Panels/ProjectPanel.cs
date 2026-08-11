using System.Diagnostics;
using System.Numerics;
using Dreambit.Editor.Assets;
using Dreambit.Editor.Logging;
using Dreambit.Editor.Projects;
using Dreambit.Editor.Scenes;
using Dreambit.Editor.Inspection;
using Dreambit.Editor.Persistence;
using Dreambit.Editor.UI;
using ImGuiNET;

namespace Dreambit.Editor.UI.Panels;

internal sealed class ProjectPanel : EditorPanel
{
    private const string CreateFolderPopup = "Create Folder##Dreambit.Editor.Project.CreateFolder";
    private const string RenamePopup = "Rename##Dreambit.Editor.Project.Rename";
    private const string MovePopup = "Move##Dreambit.Editor.Project.Move";
    private const string DeletePopup = "Delete Asset##Dreambit.Editor.Project.Delete";

    private readonly DreambitProjectDefinition _project;
    private readonly AssetDatabase _assets;
    private readonly EditorLogService _logs;
    private readonly EditorDragDropService _dragDrop;
    private readonly AssetEditingService _assetEditing;
    private readonly SceneDocumentService _scenes;
    private readonly EditorTypeRegistry _types;
    private readonly EditorWorkspaceState _workspace;
    private readonly EditorIconService _icons;
    private readonly Action<AssetRecord> _openBlueprint;
    private string _currentFolder = string.Empty;
    private string _search = string.Empty;
    private string? _selectedPath;
    private bool _selectedIsFolder;
    private string _createFolderName = "New Folder";
    private string _renameName = string.Empty;
    private string _moveDestination = string.Empty;
    private string? _pendingDeletePath;
    private string? _error;
    private bool _requestCreateFolderPopup;
    private bool _requestRenamePopup;
    private bool _requestMovePopup;
    private bool _requestDeletePopup;
    private bool _requestCreateAssetPopup;
    private Type? _createAssetType;
    private string _createAssetPath = string.Empty;
    private bool _restoreWorkspaceSelection;

    public ProjectPanel(
        DreambitProjectDefinition project,
        AssetDatabase assets,
        EditorLogService logs,
        EditorDragDropService dragDrop,
        AssetEditingService assetEditing,
        SceneDocumentService scenes,
        EditorTypeRegistry types,
        EditorWorkspaceState workspace,
        EditorIconService icons,
        Action<AssetRecord> openBlueprint)
        : base(EditorPanelIds.Project, "Project")
    {
        _project = project;
        _assets = assets;
        _logs = logs;
        _dragDrop = dragDrop;
        _assetEditing = assetEditing;
        _scenes = scenes;
        _types = types;
        _workspace = workspace;
        _icons = icons;
        _openBlueprint = openBlueprint;
        _currentFolder = workspace.ProjectBrowserFolder;
        _selectedPath = workspace.LastSelectedAssetPath;
        _selectedIsFolder = workspace.LastSelectedAssetIsFolder;
        _restoreWorkspaceSelection = string.Equals(
            workspace.LastSelectionKind,
            "asset",
            StringComparison.OrdinalIgnoreCase);
    }

    protected override void DrawContents()
    {
        var snapshot = _assets.GetSnapshot();
        EnsureCurrentFolderExists(snapshot);
        RestoreWorkspaceSelection();
        DrawToolbar();
        DrawBreadcrumbs();

        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint(
            "##ProjectSearch",
            "Search by name, path, or type",
            ref _search,
            256);

        if (!string.IsNullOrWhiteSpace(_error))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.96f, 0.44f, 0.38f, 1f));
            ImGui.TextWrapped(_error);
            ImGui.PopStyleColor();
        }

        ImGui.Separator();
        DrawAssetTable(snapshot);
        DrawStatus(snapshot);
        DrawOperationPopups();
        CaptureWorkspaceState();
    }

    private void DrawToolbar()
    {
        ImGui.BeginDisabled(_currentFolder.Length == 0);
        if (_icons.Button("ProjectUp", "folder_open", "Up one folder"))
        {
            _currentFolder = GetParentPath(_currentFolder);
            ClearSelection();
        }
        ImGui.EndDisabled();

        ImGui.SameLine();
        if (_icons.Button("ProjectCreate", "add", "Create asset or folder"))
            ImGui.OpenPopup("ProjectCreateMenu##Dreambit.Editor.Project");

        if (ImGui.BeginPopup("ProjectCreateMenu##Dreambit.Editor.Project"))
        {
            if (ImGui.MenuItem("New Folder"))
            {
                _createFolderName = "New Folder";
                _error = null;
                _requestCreateFolderPopup = true;
            }
            ImGui.Separator();
            if (ImGui.MenuItem("Entity Blueprint"))
                RequestCreateAsset(typeof(EntityBlueprint));
            if (ImGui.BeginMenu("Dreambit Asset"))
            {
                foreach (var type in _types.AssetTypes.Where(type => type != typeof(EntityBlueprint)))
                    if (ImGui.MenuItem(type.Name))
                        RequestCreateAsset(type);
                ImGui.EndMenu();
            }
            ImGui.EndPopup();
        }

        ImGui.SameLine();
        if (_icons.Button("ProjectRefresh", "refresh", "Refresh project files"))
        {
            try
            {
                _assets.RefreshNow();
                _error = null;
            }
            catch (Exception exception)
            {
                SetError($"Refresh failed. {exception.Message}");
            }
        }

        ImGui.SameLine();
        if (_icons.Button("ProjectReveal", "inventory_2", "Reveal in file browser"))
            RevealPath(_selectedPath ?? _currentFolder, _selectedPath is not null && !_selectedIsFolder);
    }

    private void DrawBreadcrumbs()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(3f, 4f));
        if (ImGui.SmallButton($"Assets##ProjectBreadcrumbRoot"))
        {
            _currentFolder = string.Empty;
            ClearSelection();
        }
        DrawDropTarget(string.Empty);

        var path = string.Empty;
        foreach (var segment in _currentFolder.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            path = path.Length == 0 ? segment : $"{path}/{segment}";
            ImGui.SameLine();
            ImGui.TextDisabled("/");
            ImGui.SameLine();
            if (ImGui.SmallButton($"{segment}##ProjectBreadcrumb:{path}"))
            {
                _currentFolder = path;
                ClearSelection();
            }
            DrawDropTarget(path);
        }
        ImGui.PopStyleVar();
    }

    private void DrawAssetTable(AssetDatabaseSnapshot snapshot)
    {
        var footerHeight = ImGui.GetTextLineHeightWithSpacing() + 8f;
        if (!ImGui.BeginChild(
                "##ProjectAssets",
                new Vector2(0f, -footerHeight),
                ImGuiChildFlags.None))
        {
            ImGui.EndChild();
            return;
        }

        var tableFlags = ImGuiTableFlags.RowBg |
                         ImGuiTableFlags.BordersInnerH |
                         ImGuiTableFlags.Resizable |
                         ImGuiTableFlags.ScrollY |
                         ImGuiTableFlags.SizingStretchProp;
        if (ImGui.BeginTable("##ProjectAssetTable", 4, tableFlags))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 0.52f);
            ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthStretch, 0.20f);
            ImGui.TableSetupColumn("Modified", ImGuiTableColumnFlags.WidthStretch, 0.18f);
            ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthStretch, 0.10f);
            ImGui.TableHeadersRow();

            if (string.IsNullOrWhiteSpace(_search))
            {
                foreach (var folder in snapshot.Folders.Where(folder =>
                             PathEquals(folder.ParentPath, _currentFolder)))
                {
                    DrawFolderRow(folder);
                }
            }

            var records = string.IsNullOrWhiteSpace(_search)
                ? snapshot.Assets.Where(asset => PathEquals(asset.FolderPath, _currentFolder))
                : _assets.Search(_search, _currentFolder, recursive: true);
            foreach (var asset in records)
                DrawAssetRow(asset);

            ImGui.EndTable();
        }

        if (ImGui.BeginPopupContextWindow(
                "ProjectEmptyContext##Dreambit.Editor.Project",
                ImGuiPopupFlags.MouseButtonRight | ImGuiPopupFlags.NoOpenOverItems))
        {
            if (ImGui.MenuItem("New Folder"))
            {
                _createFolderName = "New Folder";
                _requestCreateFolderPopup = true;
            }
            ImGui.EndPopup();
        }

        ImGui.EndChild();
    }

    private void DrawFolderRow(AssetFolderRecord folder)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        var selected = _selectedIsFolder && PathEquals(_selectedPath, folder.RelativePath);
        if (ImGui.Selectable(
                $"    {folder.Name}##folder:{folder.RelativePath}",
                selected,
                ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick))
        {
            _selectedPath = folder.RelativePath;
            _selectedIsFolder = true;
            _workspace.LastSelectionKind = "asset";
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                _currentFolder = folder.RelativePath;
                ClearSelection();
            }
        }
        DrawRowIcon("folder", new Vector4(0.95f, 0.72f, 0.28f, 1f));

        DrawDragSource(new ProjectItemDragPayload(
            folder.RelativePath,
            true,
            AssetId.Empty,
            AssetKind.Unknown,
            null));
        DrawDropTarget(folder.RelativePath);
        DrawItemContextMenu(folder.RelativePath, isFolder: true);
        ImGui.TableSetColumnIndex(1);
        ImGui.TextDisabled("Folder");
    }

    private void DrawAssetRow(AssetRecord asset)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        var selected = !_selectedIsFolder && PathEquals(_selectedPath, asset.RelativePath);
        if (ImGui.Selectable(
                $"    {asset.Name}##asset:{asset.Id}",
                selected,
                ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick))
        {
            _selectedPath = asset.RelativePath;
            _selectedIsFolder = false;
            _workspace.LastSelectionKind = "asset";
            _scenes.Selection.Clear();
            _assetEditing.Select(asset);
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) && asset.Kind == AssetKind.Blueprint)
            {
                _openBlueprint(asset);
                _error = null;
            }
            else if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) && asset.Kind == AssetKind.Scene)
            {
                try
                {
                    _scenes.Open(asset.RelativePath);
                    _assetEditing.Clear();
                    _error = null;
                }
                catch (Exception exception)
                {
                    SetError($"Could not open scene. {exception.Message}");
                }
            }
        }
        DrawRowIcon(GetAssetIcon(asset.Kind));

        DrawDragSource(new ProjectItemDragPayload(
            asset.RelativePath,
            false,
            asset.Id,
            asset.Kind,
            asset.TypeName));
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(asset.RelativePath);
            ImGui.TextDisabled($"ID  {asset.Id}");
            if (!string.IsNullOrWhiteSpace(asset.TypeName))
                ImGui.TextDisabled(asset.TypeName);
            ImGui.EndTooltip();
        }

        DrawItemContextMenu(asset.RelativePath, isFolder: false);
        ImGui.TableSetColumnIndex(1);
        ImGui.TextUnformatted(GetKindLabel(asset.Kind));
        ImGui.TableSetColumnIndex(2);
        ImGui.TextDisabled(asset.LastWriteUtc.ToLocalTime().ToString("g"));
        ImGui.TableSetColumnIndex(3);
        ImGui.TextDisabled(FormatSize(asset.Length));
    }

    private void DrawRowIcon(string icon, Vector4? tint = null)
    {
        var minimum = ImGui.GetItemRectMin();
        var maximum = ImGui.GetItemRectMax();
        var size = MathF.Min(17f, maximum.Y - minimum.Y - 2f);
        _icons.DrawAt(
            ImGui.GetWindowDrawList(),
            icon,
            minimum + new Vector2(4f, (maximum.Y - minimum.Y - size) * 0.5f),
            new Vector2(size),
            tint);
    }

    private static string GetAssetIcon(AssetKind kind) => kind switch
    {
        AssetKind.Blueprint => "view_in_ar",
        AssetKind.Scene => "layers",
        AssetKind.Texture => "image",
        AssetKind.Sprite => "palette",
        AssetKind.SpriteSheet => "image",
        AssetKind.Animation => "animation",
        AssetKind.Audio => "audiotrack",
        AssetKind.SoundCue => "audiotrack",
        AssetKind.Ldtk => "data_object",
        _ => "extension"
    };

    private void DrawItemContextMenu(string path, bool isFolder)
    {
        if (!ImGui.BeginPopupContextItem($"ProjectItemContext##{path}"))
            return;

        _selectedPath = path;
        _selectedIsFolder = isFolder;

        if (isFolder && ImGui.MenuItem("Open"))
        {
            _currentFolder = path;
            ClearSelection();
        }

        if (ImGui.MenuItem("Rename"))
        {
            _renameName = Path.GetFileName(path);
            _error = null;
            _requestRenamePopup = true;
        }

        if (ImGui.MenuItem("Move..."))
        {
            _moveDestination = _currentFolder;
            _error = null;
            _requestMovePopup = true;
        }

        if (ImGui.MenuItem("Duplicate"))
        {
            if (!_assets.TryDuplicate(path, out var duplicatedPath, out var error))
                SetError(error ?? "Could not duplicate the selection.");
            else
            {
                _selectedPath = duplicatedPath;
                _selectedIsFolder = isFolder;
                _error = null;
            }
        }

        ImGui.Separator();
        if (ImGui.MenuItem("Copy Relative Path"))
            ImGui.SetClipboardText(path);
        if (!isFolder && _assets.TryGetAsset(path, out var asset) &&
            ImGui.MenuItem("Copy Asset ID"))
        {
            ImGui.SetClipboardText(asset!.Id.ToString());
        }
        if (ImGui.MenuItem("Reveal in File Browser"))
            RevealPath(path, !isFolder);

        ImGui.Separator();
        if (ImGui.MenuItem("Delete"))
        {
            _pendingDeletePath = path;
            _requestDeletePopup = true;
        }

        ImGui.EndPopup();
    }

    private void DrawStatus(AssetDatabaseSnapshot snapshot)
    {
        var visibleAssetCount = string.IsNullOrWhiteSpace(_search)
            ? snapshot.Assets.Count(asset => PathEquals(asset.FolderPath, _currentFolder))
            : _assets.Search(_search, _currentFolder, recursive: true).Count;
        var status = $"{visibleAssetCount} asset{(visibleAssetCount == 1 ? string.Empty : "s")}";
        if (snapshot.MissingAssetCount > 0)
            status += $"  |  {snapshot.MissingAssetCount} missing reference target(s) retained";
        ImGui.TextDisabled(status);
    }

    private void DrawDragSource(ProjectItemDragPayload payload)
    {
        if (!ImGui.BeginDragDropSource())
            return;

        _dragDrop.SetProjectItem(payload);
        ImGui.SetDragDropPayload(
            EditorDragDropService.ProjectItemPayloadType,
            IntPtr.Zero,
            0);
        ImGui.TextUnformatted(payload.RelativePath);
        ImGui.EndDragDropSource();
    }

    private unsafe void DrawDropTarget(string destinationFolder)
    {
        if (!ImGui.BeginDragDropTarget())
            return;

        var accepted = ImGui.AcceptDragDropPayload(
            EditorDragDropService.ProjectItemPayloadType);
        if (accepted.NativePtr != null && _dragDrop.ProjectItem is { } payload)
        {
            if (!_assets.TryMove(payload.RelativePath, destinationFolder, out var error))
                SetError(error ?? "Could not move the dragged item.");
            else
            {
                _selectedPath = JoinPath(destinationFolder, Path.GetFileName(payload.RelativePath));
                _selectedIsFolder = payload.IsFolder;
                _error = null;
            }

            _dragDrop.ClearProjectItem();
        }

        ImGui.EndDragDropTarget();
    }

    private void DrawOperationPopups()
    {
        OpenRequestedPopups();
        DrawCreateFolderPopup();
        DrawRenamePopup();
        DrawMovePopup();
        DrawDeletePopup();
        DrawCreateAssetPopup();
    }

    public void RequestCreateAsset(Type type)
    {
        _createAssetType = type;
        var suffix = AssetTypeClassifier.GetFileSuffix(type);
        _createAssetPath = JoinPath(_currentFolder, $"New {type.Name}{suffix}");
        _requestCreateAssetPopup = true;
    }

    private void DrawCreateAssetPopup()
    {
        if (!ImGui.BeginPopupModal(
                "Create Asset##Dreambit.Editor.Project",
                ImGuiWindowFlags.AlwaysAutoResize))
            return;
        ImGui.TextUnformatted($"Create {_createAssetType?.Name ?? "Asset"}");
        ImGui.SetNextItemWidth(480f);
        var submit = ImGui.InputText("Path", ref _createAssetPath, 1024, ImGuiInputTextFlags.EnterReturnsTrue);
        if ((submit || ImGui.Button("Create", new Vector2(90, 0))) && _createAssetType is not null)
        {
            if (_assetEditing.TryCreate(_createAssetType, _createAssetPath, out var error))
            {
                _selectedPath = _createAssetPath;
                _selectedIsFolder = false;
                _error = null;
                ImGui.CloseCurrentPopup();
            }
            else
            {
                SetError(error ?? "Could not create asset.");
            }
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(90, 0)))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private void OpenRequestedPopups()
    {
        if (_requestCreateFolderPopup)
        {
            ImGui.OpenPopup(CreateFolderPopup);
            _requestCreateFolderPopup = false;
        }
        if (_requestRenamePopup)
        {
            ImGui.OpenPopup(RenamePopup);
            _requestRenamePopup = false;
        }
        if (_requestMovePopup)
        {
            ImGui.OpenPopup(MovePopup);
            _requestMovePopup = false;
        }
        if (_requestDeletePopup)
        {
            ImGui.OpenPopup(DeletePopup);
            _requestDeletePopup = false;
        }
        if (_requestCreateAssetPopup)
        {
            ImGui.OpenPopup("Create Asset##Dreambit.Editor.Project");
            _requestCreateAssetPopup = false;
        }
    }

    private void DrawCreateFolderPopup()
    {
        if (!ImGui.BeginPopupModal(CreateFolderPopup, ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.TextUnformatted($"Create in Assets/{_currentFolder}".TrimEnd('/'));
        ImGui.SetNextItemWidth(360f);
        ImGui.InputText("Name", ref _createFolderName, 256);
        DrawPopupError();
        if (ImGui.Button("Create", new Vector2(90f, 0f)))
        {
            if (_assets.TryCreateFolder(_currentFolder, _createFolderName, out var error))
            {
                _error = null;
                ImGui.CloseCurrentPopup();
            }
            else
                SetError(error ?? "Could not create the folder.");
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(90f, 0f)))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private void DrawRenamePopup()
    {
        if (!ImGui.BeginPopupModal(RenamePopup, ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.SetNextItemWidth(360f);
        ImGui.InputText("Name", ref _renameName, 256);
        DrawPopupError();
        if (ImGui.Button("Rename", new Vector2(90f, 0f)) && _selectedPath is not null)
        {
            if (_assets.TryRename(_selectedPath, _renameName, out var error))
            {
                _selectedPath = JoinPath(GetParentPath(_selectedPath), _renameName.Trim());
                _error = null;
                ImGui.CloseCurrentPopup();
            }
            else
                SetError(error ?? "Could not rename the selection.");
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(90f, 0f)))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private void DrawMovePopup()
    {
        if (!ImGui.BeginPopupModal(MovePopup, ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.TextWrapped("Destination folder, relative to Content/Assets. Leave empty for the root.");
        ImGui.SetNextItemWidth(460f);
        ImGui.InputTextWithHint("##MoveDestination", "characters/player", ref _moveDestination, 512);
        DrawPopupError();
        if (ImGui.Button("Move", new Vector2(90f, 0f)) && _selectedPath is not null)
        {
            var name = Path.GetFileName(_selectedPath);
            if (_assets.TryMove(_selectedPath, _moveDestination, out var error))
            {
                _selectedPath = JoinPath(_moveDestination.Trim().Replace('\\', '/').Trim('/'), name);
                _error = null;
                ImGui.CloseCurrentPopup();
            }
            else
                SetError(error ?? "Could not move the selection.");
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(90f, 0f)))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private void DrawDeletePopup()
    {
        if (!ImGui.BeginPopupModal(DeletePopup, ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.TextWrapped($"Delete '{_pendingDeletePath}' from disk?");
        ImGui.TextDisabled("Its stable asset ID will remain as a missing-reference tombstone.");
        DrawPopupError();
        ImGui.Spacing();
        if (ImGui.Button("Delete", new Vector2(90f, 0f)) && _pendingDeletePath is not null)
        {
            if (_assets.TryDelete(_pendingDeletePath, out var error))
            {
                ClearSelection();
                _pendingDeletePath = null;
                _error = null;
                ImGui.CloseCurrentPopup();
            }
            else
                SetError(error ?? "Could not delete the selection.");
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(90f, 0f)))
        {
            _pendingDeletePath = null;
            ImGui.CloseCurrentPopup();
        }
        ImGui.EndPopup();
    }

    private void RevealPath(string relativePath, bool selectFile)
    {
        try
        {
            var absolutePath = Path.Combine(
                _project.ContentRootPath,
                relativePath.Replace('/', Path.DirectorySeparatorChar));
            ProcessStartInfo startInfo;
            if (OperatingSystem.IsWindows())
            {
                var arguments = selectFile && File.Exists(absolutePath)
                    ? $"/select,\"{absolutePath}\""
                    : $"\"{(Directory.Exists(absolutePath) ? absolutePath : Path.GetDirectoryName(absolutePath))}\"";
                startInfo = new ProcessStartInfo("explorer.exe", arguments)
                {
                    UseShellExecute = true
                };
            }
            else if (OperatingSystem.IsMacOS())
            {
                startInfo = new ProcessStartInfo("open", $"\"{absolutePath}\"")
                {
                    UseShellExecute = false
                };
            }
            else
            {
                var folder = Directory.Exists(absolutePath)
                    ? absolutePath
                    : Path.GetDirectoryName(absolutePath)!;
                startInfo = new ProcessStartInfo("xdg-open", $"\"{folder}\"")
                {
                    UseShellExecute = false
                };
            }

            Process.Start(startInfo)?.Dispose();
        }
        catch (Exception exception)
        {
            SetError($"Could not reveal the path. {exception.Message}");
        }
    }

    private void EnsureCurrentFolderExists(AssetDatabaseSnapshot snapshot)
    {
        if (_currentFolder.Length == 0 || snapshot.Folders.Any(folder =>
                PathEquals(folder.RelativePath, _currentFolder)))
        {
            return;
        }

        _currentFolder = string.Empty;
        ClearSelection();
    }

    private void ClearSelection()
    {
        _selectedPath = null;
        _selectedIsFolder = false;
    }

    private void RestoreWorkspaceSelection()
    {
        if (!_restoreWorkspaceSelection)
            return;
        _restoreWorkspaceSelection = false;
        if (_selectedPath is null || _selectedIsFolder)
            return;
        if (_assets.TryGetAsset(_selectedPath, out var asset))
            _assetEditing.Select(asset);
        else
            ClearSelection();
    }

    private void CaptureWorkspaceState()
    {
        _workspace.ProjectBrowserFolder = _currentFolder;
        _workspace.LastSelectedAssetPath = _selectedPath;
        _workspace.LastSelectedAssetIsFolder = _selectedIsFolder;
        if (_assetEditing.Selected is not null || _selectedPath is not null)
            _workspace.LastSelectionKind = "asset";
    }

    private void SetError(string message)
    {
        _error = message;
        _logs.Warning("Assets", message);
    }

    private void DrawPopupError()
    {
        if (string.IsNullOrWhiteSpace(_error))
            return;

        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.96f, 0.44f, 0.38f, 1f));
        ImGui.TextWrapped(_error);
        ImGui.PopStyleColor();
    }

    private static string GetParentPath(string path)
    {
        var normalized = path.Replace('\\', '/').Trim('/');
        var separator = normalized.LastIndexOf('/');
        return separator < 0 ? string.Empty : normalized[..separator];
    }

    private static string JoinPath(string parent, string name) =>
        string.IsNullOrWhiteSpace(parent) ? name : $"{parent.TrimEnd('/')}/{name}";

    private static bool PathEquals(string? left, string? right) =>
        StringComparer.OrdinalIgnoreCase.Equals(left ?? string.Empty, right ?? string.Empty);

    private static string GetKindLabel(AssetKind kind) => kind switch
    {
        AssetKind.SpriteSheet => "Sprite Sheet",
        AssetKind.SoundCue => "Sound Cue",
        AssetKind.ParticleEffect => "Particle Effect",
        AssetKind.TiledMap => "Tiled Map",
        _ => kind.ToString()
    };

    private static string FormatSize(long bytes)
    {
        if (bytes < 1_024)
            return $"{bytes} B";
        if (bytes < 1_048_576)
            return $"{bytes / 1_024d:0.#} KB";
        if (bytes < 1_073_741_824)
            return $"{bytes / 1_048_576d:0.#} MB";
        return $"{bytes / 1_073_741_824d:0.#} GB";
    }
}
