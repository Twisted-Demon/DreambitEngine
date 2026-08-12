using System.Numerics;
using Dreambit.ECS;
using Dreambit.Editor.Assets;
using Dreambit.Editor.Graphics;
using Dreambit.Editor.Persistence;
using Dreambit.Editor.Scenes;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace Dreambit.Editor.UI.Panels;

internal sealed class BlueprintViewPanel : EditorPanel
{
    private const string ViewSettingsPopup = "Blueprint View Settings##Dreambit.Editor.BlueprintView";
    private readonly AssetDatabase _assets;
    private readonly AssetEditingService _assetEditing;
    private readonly BlueprintEditingService _blueprints;
    private readonly EditorDocumentContext _documentContext;
    private readonly EditorWorkspaceState _workspace;
    private readonly SceneViewportRenderer _renderer;
    private readonly EditorIconService _icons;
    private AssetRecord? _asset;
    private DateTimeOffset _sourceWriteUtc;
    private bool _needsRebuild;
    private bool _viewSettingsRequested;
    private string? _error;

    public BlueprintViewPanel(
        AssetDatabase assets,
        AssetEditingService assetEditing,
        BlueprintEditingService blueprints,
        EditorDocumentContext documentContext,
        EditorWorkspaceState workspace,
        SceneViewportRenderer renderer,
        EditorIconService icons)
        : base(EditorPanelIds.Blueprint, "Blueprint View", isOpenByDefault: false)
    {
        _assets = assets;
        _assetEditing = assetEditing;
        _blueprints = blueprints;
        _documentContext = documentContext;
        _workspace = workspace;
        _renderer = renderer;
        _icons = icons;
        _assetEditing.Changed += OnAssetDocumentChanged;
        _assetEditing.PreviewChanged += OnAssetPreviewChanged;

        if (!string.IsNullOrWhiteSpace(workspace.LastBlueprintPath) &&
            assets.TryGetAsset(workspace.LastBlueprintPath, out var restored) &&
            restored!.Kind == AssetKind.Blueprint)
        {
            _asset = restored;
            _needsRebuild = true;
        }
    }

    protected override ImGuiWindowFlags WindowFlags =>
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

    public void Open(AssetRecord asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        if (asset.Kind != AssetKind.Blueprint)
            return;
        _asset = asset;
        _workspace.LastBlueprintPath = asset.RelativePath;
        IsOpen = true;
        _needsRebuild = true;
        _assetEditing.Select(asset);
    }

    protected override void DrawContents()
    {
        _documentContext.ActivateBlueprint();
        RefreshAssetRecord();
        if (_needsRebuild)
            RebuildPreview();

        DrawToolbar();
        ImGui.Separator();
        var canvasPosition = ImGui.GetCursorScreenPos();
        var canvasSize = ImGui.GetContentRegionAvail();
        canvasSize.X = MathF.Max(1f, canvasSize.X);
        canvasSize.Y = MathF.Max(1f, canvasSize.Y);

        if (_asset is null)
        {
            DrawEmptyCanvas(canvasPosition, canvasSize, "No Blueprint is open", "Double-click a Blueprint in Project.");
            return;
        }
        var scene = _blueprints.Current?.Scene;
        if (scene is null)
        {
            DrawEmptyCanvas(
                canvasPosition,
                canvasSize,
                "Blueprint preview unavailable",
                _blueprints.Error ?? _error ?? "The preview will return after game code reloads.");
            return;
        }

        scene.EditorTick();
        var camera = _renderer.Render(
            scene,
            (int)MathF.Ceiling(canvasSize.X),
            (int)MathF.Ceiling(canvasSize.Y),
            new XnaVector2(_workspace.BlueprintCameraX, _workspace.BlueprintCameraY),
            EditorViewportUi.NormalizeZoom(_workspace.BlueprintCameraZoom));
        ImGui.Image(_renderer.TextureId, canvasSize);

        var hovered = ImGui.IsItemHovered();
        var active = ImGui.IsItemActive();
        var mouseLocal = ImGui.GetMousePos() - canvasPosition;
        var drawList = ImGui.GetWindowDrawList();
        if (_workspace.ShowGrid)
        {
            EditorViewportUi.DrawGrid(
                drawList,
                camera,
                canvasPosition,
                canvasSize,
                _workspace.GridSize);
        }
        scene.DrawEditorGizmos(
            new PreviewGizmoContext(drawList, camera, canvasPosition),
            _blueprints.Selection.EntityIds.ToHashSet());
        if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left) &&
            !ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            var world = camera.ScreenToWorld(new XnaVector2(mouseLocal.X, mouseLocal.Y));
            _blueprints.Selection.Set(_renderer.Pick(scene, world), ImGui.GetIO().KeyCtrl);
        }
        HandleCameraInput(camera, mouseLocal, canvasSize, hovered, active);

        var renderError = _renderer.LastError ?? _error;
        if (!string.IsNullOrWhiteSpace(renderError))
        {
            drawList.AddRectFilled(
                canvasPosition + new Vector2(8f),
                canvasPosition + new Vector2(canvasSize.X - 8f, 38f),
                ImGui.GetColorU32(new Vector4(0.25f, 0.06f, 0.07f, 0.92f)));
            drawList.AddText(
                canvasPosition + new Vector2(16f, 15f),
                ImGui.GetColorU32(new Vector4(1f, 0.55f, 0.58f, 1f)),
                renderError);
        }
    }

    private void DrawToolbar()
    {
        if (_viewSettingsRequested)
        {
            ImGui.OpenPopup(ViewSettingsPopup);
            _viewSettingsRequested = false;
        }
        ImGui.TextDisabled(_asset?.RelativePath ?? "Blueprint");
        ImGui.SameLine();
        if (_icons.Button("FrameBlueprint", "center_focus_strong", "Frame Blueprint (F)"))
            FrameBlueprint();
        ImGui.SameLine();
        if (_icons.Button("BlueprintGrid", "grid_on", "Toggle grid", _workspace.ShowGrid))
            _workspace.ShowGrid = !_workspace.ShowGrid;
        ImGui.SameLine();
        if (_icons.Button("BlueprintSettings", "settings", "Grid and snapping settings"))
            _viewSettingsRequested = true;
        EditorViewportUi.DrawSettingsPopup(ViewSettingsPopup, _workspace);
        ImGui.SameLine();
        ImGui.TextDisabled($"Zoom {_workspace.BlueprintCameraZoom:0.00}x");
    }

    private void HandleCameraInput(
        Camera2D camera,
        Vector2 mouseLocal,
        Vector2 canvasSize,
        bool hovered,
        bool active)
    {
        if (!hovered && !active)
            return;
        var io = ImGui.GetIO();
        if (hovered && MathF.Abs(io.MouseWheel) > float.Epsilon)
        {
            var previousWorld = camera.ScreenToWorld(new XnaVector2(mouseLocal.X, mouseLocal.Y));
            var nextZoom = EditorViewportUi.ApplyZoomWheel(_workspace.BlueprintCameraZoom, io.MouseWheel);
            var nextScale = camera.Scale *
                            (nextZoom / EditorViewportUi.NormalizeZoom(_workspace.BlueprintCameraZoom));
            var offset = new XnaVector2(
                mouseLocal.X - canvasSize.X * 0.5f,
                mouseLocal.Y - canvasSize.Y * 0.5f) / nextScale;
            var nextPosition = previousWorld - offset;
            _workspace.BlueprintCameraX = nextPosition.X;
            _workspace.BlueprintCameraY = nextPosition.Y;
            _workspace.BlueprintCameraZoom = nextZoom;
        }
        if (ImGui.IsMouseDragging(ImGuiMouseButton.Middle) ||
            ImGui.IsMouseDragging(ImGuiMouseButton.Right))
        {
            _workspace.BlueprintCameraX -= io.MouseDelta.X / camera.Scale;
            _workspace.BlueprintCameraY -= io.MouseDelta.Y / camera.Scale;
        }
        if (hovered && ImGui.IsKeyPressed(ImGuiKey.F))
            FrameBlueprint();
    }

    private void FrameBlueprint()
    {
        var scene = _blueprints.Current?.Scene;
        if (scene is null)
            return;
        var selected = _blueprints.Selection.GetActive(scene);
        if (selected is not null)
        {
            _workspace.BlueprintCameraX = selected.Transform.WorldPosition.X;
            _workspace.BlueprintCameraY = selected.Transform.WorldPosition.Y;
            return;
        }
        var roots = scene.GetAllEntities().Where(entity => entity.Parent is null).ToArray();
        if (roots.Length == 0)
            return;
        var center = roots.Aggregate(XnaVector2.Zero, (sum, entity) => sum + entity.Transform.WorldPosition2D) /
                     roots.Length;
        _workspace.BlueprintCameraX = center.X;
        _workspace.BlueprintCameraY = center.Y;
    }

    private void RefreshAssetRecord()
    {
        if (_asset is null)
            return;
        var current = _assets.GetSnapshot().Assets.FirstOrDefault(asset => asset.Id == _asset.Id);
        if (current is null)
            return;
        if (current.LastWriteUtc != _sourceWriteUtc &&
            _assetEditing.Current?.Asset.Id != current.Id)
        {
            _needsRebuild = true;
        }
        _asset = current;
    }

    private void RebuildPreview()
    {
        _needsRebuild = false;
        if (_asset is null)
            return;
        try
        {
            _blueprints.Open(_asset);
            _sourceWriteUtc = _asset.LastWriteUtc;
            _error = null;
        }
        catch (Exception exception)
        {
            _error = exception.Message;
        }
    }

    private void OnAssetDocumentChanged()
    {
        if (_assetEditing.Current?.Asset.Id == _asset?.Id)
            _needsRebuild = true;
    }

    private void OnAssetPreviewChanged(DreambitAssetDocument document)
    {
        if (document.Asset.Id == _asset?.Id)
            _needsRebuild = true;
    }

    private static void DrawEmptyCanvas(Vector2 position, Vector2 size, string title, string detail)
    {
        ImGui.InvisibleButton("##BlueprintCanvas", size);
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(position, position + size, ImGui.GetColorU32(new Vector4(0.075f, 0.082f, 0.095f, 1f)));
        var center = position + size * 0.5f;
        var titleSize = ImGui.CalcTextSize(title);
        var detailSize = ImGui.CalcTextSize(detail);
        drawList.AddText(center - new Vector2(titleSize.X * 0.5f, 18f), ImGui.GetColorU32(new Vector4(0.82f, 0.84f, 0.88f, 1f)), title);
        drawList.AddText(center - new Vector2(detailSize.X * 0.5f, -6f), ImGui.GetColorU32(new Vector4(0.50f, 0.53f, 0.59f, 1f)), detail);
    }

    protected override void DisposeCore()
    {
        _assetEditing.Changed -= OnAssetDocumentChanged;
        _assetEditing.PreviewChanged -= OnAssetPreviewChanged;
        _blueprints.Dispose();
        _renderer.Dispose();
    }

    private sealed class PreviewGizmoContext(
        ImDrawListPtr drawList,
        Camera2D camera,
        Vector2 canvasPosition) : IEditorGizmoContext
    {
        public void Line(XnaVector2 from, XnaVector2 to, Color color, float thickness = 1f) =>
            drawList.AddLine(Screen(from), Screen(to), ColorU32(color), MathF.Max(1f, thickness));

        public void Circle(XnaVector2 center, float radius, Color color, float thickness = 1f) =>
            drawList.AddCircle(Screen(center), MathF.Abs(radius * camera.Scale), ColorU32(color), 48, MathF.Max(1f, thickness));

        public void Rectangle(RectangleF rectangle, Color color, float thickness = 1f) =>
            drawList.AddRect(
                Screen(new XnaVector2(rectangle.Left, rectangle.Top)),
                Screen(new XnaVector2(rectangle.Right, rectangle.Bottom)),
                ColorU32(color),
                0f,
                ImDrawFlags.None,
                MathF.Max(1f, thickness));

        public void Label(XnaVector2 position, string text, Color color)
        {
            if (!string.IsNullOrWhiteSpace(text))
                drawList.AddText(Screen(position), ColorU32(color), text);
        }

        private Vector2 Screen(XnaVector2 world)
        {
            var screen = camera.WorldToScreen(world);
            return canvasPosition + new Vector2(screen.X, screen.Y);
        }

        private static uint ColorU32(Color color) => ImGui.GetColorU32(
            new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f));
    }
}
