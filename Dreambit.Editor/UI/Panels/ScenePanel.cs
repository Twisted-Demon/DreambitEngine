using Dreambit.ECS;
using Dreambit.Editor.Graphics;
using Dreambit.Editor.Persistence;
using Dreambit.Editor.Scenes;
using Dreambit.Editor.Assets;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace Dreambit.Editor.UI.Panels;

internal sealed class ScenePanel : EditorPanel
{
    private const string ViewSettingsPopup = "Scene View Settings##Dreambit.Editor.Scene";
    private readonly SceneDocumentService _documents;
    private readonly SelectionService _selection;
    private readonly EditorWorkspaceState _workspace;
    private readonly SceneViewportRenderer _renderer;
    private readonly EditorDragDropService _dragDrop;
    private readonly AssetDatabase _assets;
    private readonly EditorIconService _icons;
    private GizmoDrag? _gizmoDrag;
    private ColliderDrag? _colliderDrag;
    private PointLightDrag? _pointLightDrag;
    private bool _viewSettingsRequested;

    public ScenePanel(
        SceneDocumentService documents,
        SelectionService selection,
        EditorWorkspaceState workspace,
        SceneViewportRenderer renderer,
        EditorDragDropService dragDrop,
        AssetDatabase assets,
        EditorIconService icons)
        : base(EditorPanelIds.Scene, "Scene")
    {
        _documents = documents;
        _selection = selection;
        _workspace = workspace;
        _renderer = renderer;
        _dragDrop = dragDrop;
        _assets = assets;
        _icons = icons;
    }

    protected override ImGuiWindowFlags WindowFlags =>
        ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse;

    protected override void DrawContents()
    {
        DrawToolbar();
        ImGui.Separator();

        var canvasPosition = ImGui.GetCursorScreenPos();
        var canvasSize = ImGui.GetContentRegionAvail();
        canvasSize.X = MathF.Max(canvasSize.X, 1f);
        canvasSize.Y = MathF.Max(canvasSize.Y, 1f);
        var document = _documents.Current;
        if (document?.Scene is not { } scene)
        {
            DrawEmptyCanvas(canvasPosition, canvasSize, "No scene is open", "Use File > New Scene or Open Scene.");
            return;
        }

        var cameraPosition = new XnaVector2(_workspace.SceneCameraX, _workspace.SceneCameraY);
        var camera = _renderer.Render(
            scene,
            (int)MathF.Ceiling(canvasSize.X),
            (int)MathF.Ceiling(canvasSize.Y),
            cameraPosition,
            EditorViewportUi.NormalizeZoom(_workspace.SceneCameraZoom));

        ImGui.Image(_renderer.TextureId, canvasSize);
        var hovered = ImGui.IsItemHovered();
        var active = ImGui.IsItemActive();
        var mouseLocal = ImGui.GetMousePos() - canvasPosition;
        DrawAssetDropTarget(document, camera, mouseLocal);
        var drawList = ImGui.GetWindowDrawList();
        if (_workspace.ShowGrid)
            EditorViewportUi.DrawGrid(
                drawList,
                camera,
                canvasPosition,
                canvasSize,
                _workspace.GridSize);
        scene.DrawEditorGizmos(
            new ImGuiEditorGizmoContext(drawList, camera, canvasPosition),
            document.Selection.EntityIds.ToHashSet());
        DrawSelection(drawList, camera, canvasPosition, canvasSize);
        var componentConsumed = DrawComponentHandles(
            drawList,
            document,
            camera,
            canvasPosition,
            mouseLocal,
            hovered);
        var gizmoConsumed = !componentConsumed && DrawAndHandleGizmo(
            drawList,
            document,
            camera,
            canvasPosition,
            mouseLocal,
            hovered);
        HandleCameraInput(
            scene,
            camera,
            mouseLocal,
            canvasSize,
            hovered,
            active,
            !gizmoConsumed && !componentConsumed);

        if (!string.IsNullOrWhiteSpace(_renderer.LastError))
        {
            drawList.AddRectFilled(
                canvasPosition + new Vector2(8, 8),
                canvasPosition + new Vector2(canvasSize.X - 8, 38),
                ImGui.GetColorU32(new Vector4(0.25f, 0.06f, 0.07f, 0.92f)));
            drawList.AddText(
                canvasPosition + new Vector2(16, 15),
                ImGui.GetColorU32(new Vector4(1f, 0.55f, 0.58f, 1f)),
                $"Scene preview: {_renderer.LastError}");
        }
    }

    private unsafe void DrawAssetDropTarget(
        SceneDocument document,
        Camera2D camera,
        Vector2 mouseLocal)
    {
        if (!ImGui.BeginDragDropTarget())
            return;
        var accepted = ImGui.AcceptDragDropPayload(EditorDragDropService.ProjectItemPayloadType);
        if (accepted.NativePtr != null && _dragDrop.ProjectItem is { Kind: AssetKind.Blueprint } payload &&
            _assets.TryGetAsset(payload.RelativePath, out var asset))
        {
            try
            {
                var path = Path.Combine(
                    _assets.ContentRoot,
                    asset!.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                var source = DreambitJson.Deserialize<EntityBlueprint>(File.ReadAllText(path))
                             ?? throw new InvalidDataException("Blueprint file is empty.");
                source.AssetId = asset.Id;
                source.AssetName = asset.LogicalAssetName;
                var world = camera.ScreenToWorld(new XnaVector2(mouseLocal.X, mouseLocal.Y));
                document.InstantiateBlueprint(
                    source,
                    new Microsoft.Xna.Framework.Vector3(world, source.Position.Z));
            }
            finally
            {
                _dragDrop.ClearProjectItem();
            }
        }
        ImGui.EndDragDropTarget();
    }

    private void HandleCameraInput(
        Scene scene,
        Camera2D camera,
        Vector2 mouseLocal,
        Vector2 canvasSize,
        bool hovered,
        bool active,
        bool allowSelection)
    {
        if (!hovered && !active)
            return;
        var io = ImGui.GetIO();
        if (hovered && MathF.Abs(io.MouseWheel) > float.Epsilon)
        {
            var previousWorld = camera.ScreenToWorld(new XnaVector2(mouseLocal.X, mouseLocal.Y));
            var nextZoom = EditorViewportUi.ApplyZoomWheel(_workspace.SceneCameraZoom, io.MouseWheel);
            var nextScale = camera.Scale *
                            (nextZoom / EditorViewportUi.NormalizeZoom(_workspace.SceneCameraZoom));
            var offset = new XnaVector2(
                mouseLocal.X - canvasSize.X * 0.5f,
                mouseLocal.Y - canvasSize.Y * 0.5f) / nextScale;
            var nextPosition = previousWorld - offset;
            _workspace.SceneCameraX = nextPosition.X;
            _workspace.SceneCameraY = nextPosition.Y;
            _workspace.SceneCameraZoom = nextZoom;
        }

        if (ImGui.IsMouseDragging(ImGuiMouseButton.Middle) ||
            ImGui.IsMouseDragging(ImGuiMouseButton.Right))
        {
            _workspace.SceneCameraX -= io.MouseDelta.X / camera.Scale;
            _workspace.SceneCameraY -= io.MouseDelta.Y / camera.Scale;
        }

        if (allowSelection && hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left) &&
            !ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            var world = camera.ScreenToWorld(new XnaVector2(mouseLocal.X, mouseLocal.Y));
            _selection.Set(_renderer.Pick(scene, world), io.KeyCtrl);
        }

        if (hovered && ImGui.IsKeyPressed(ImGuiKey.F))
            FrameSelected();
    }

    private void DrawToolbar()
    {
        if (_viewSettingsRequested)
        {
            ImGui.OpenPopup(ViewSettingsPopup);
            _viewSettingsRequested = false;
        }
        if (_icons.Button("Select", "mouse", "Select (Q)", _workspace.GizmoMode == 0))
            _workspace.GizmoMode = 0;
        ImGui.SameLine();
        if (_icons.Button("Move", "open_with", "Move (W)", _workspace.GizmoMode == 1))
            _workspace.GizmoMode = 1;
        ImGui.SameLine();
        if (_icons.Button("Rotate", "rotate_right", "Rotate (E)", _workspace.GizmoMode == 2))
            _workspace.GizmoMode = 2;
        ImGui.SameLine();
        if (_icons.Button("Scale", "aspect_ratio", "Scale (R)", _workspace.GizmoMode == 3))
            _workspace.GizmoMode = 3;
        ImGui.SameLine();
        if (_icons.Button("Frame", "center_focus_strong", "Frame selected (F)"))
            FrameSelected();
        ImGui.SameLine();
        if (_icons.Button("Grid", "grid_on", "Toggle grid", _workspace.ShowGrid))
            _workspace.ShowGrid = !_workspace.ShowGrid;
        ImGui.SameLine();
        var snap = _workspace.SnapEnabled;
        if (ImGui.Checkbox("Snap", ref snap))
            _workspace.SnapEnabled = snap;
        if (_workspace.SnapEnabled)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(70f);
            if (_workspace.GizmoMode == 2)
            {
                var value = _workspace.RotateSnapDegrees;
                if (ImGui.DragFloat("##SnapValue", ref value, 1f, 1f, 180f, "%.0f°"))
                    _workspace.RotateSnapDegrees = value;
            }
            else if (_workspace.GizmoMode == 3)
            {
                var value = _workspace.ScaleSnap;
                if (ImGui.DragFloat("##SnapValue", ref value, 0.01f, 0.01f, 10f))
                    _workspace.ScaleSnap = value;
            }
            else
            {
                var value = _workspace.MoveSnap;
                if (ImGui.DragFloat("##SnapValue", ref value, 0.1f, 0.01f, 1000f))
                    _workspace.MoveSnap = value;
            }
        }
        ImGui.SameLine();
        if (_icons.Button("ViewSettings", "settings", "Grid and snapping settings"))
            _viewSettingsRequested = true;
        EditorViewportUi.DrawSettingsPopup(ViewSettingsPopup, _workspace);
        ImGui.SameLine();
        ImGui.TextDisabled($"Zoom {_workspace.SceneCameraZoom:0.00}x");
    }

    private void FrameSelected()
    {
        var entity = _selection.GetActive(_documents.Current?.Scene);
        if (entity is null)
            return;
        var position = entity.Transform.WorldPosition2D;
        _workspace.SceneCameraX = position.X;
        _workspace.SceneCameraY = position.Y;
    }


    private void DrawSelection(
        ImDrawListPtr drawList,
        Camera2D camera,
        Vector2 canvasPosition,
        Vector2 canvasSize)
    {
        var color = ImGui.GetColorU32(new Vector4(0.24f, 0.65f, 1f, 1f));
        foreach (var entity in _selection.Resolve(_documents.Current?.Scene))
        {
            RectangleF? bounds = null;
            foreach (var drawable in entity.GetAllComponents().OfType<DrawableComponent>())
            {
                try
                {
                    bounds = bounds is null ? drawable.Bounds : RectangleF.Union(bounds.Value, drawable.Bounds);
                }
                catch
                {
                }
            }
            if (bounds is null)
            {
                var center = camera.WorldToScreen(entity.Transform.WorldPosition2D);
                var point = canvasPosition + new Vector2(center.X, center.Y);
                drawList.AddCircle(point, 6f, color, 16, 2f);
                continue;
            }
            var topLeft = camera.WorldToScreen(new XnaVector2(bounds.Value.Left, bounds.Value.Top));
            var bottomRight = camera.WorldToScreen(new XnaVector2(bounds.Value.Right, bounds.Value.Bottom));
            drawList.AddRect(
                canvasPosition + new Vector2(topLeft.X, topLeft.Y),
                canvasPosition + new Vector2(bottomRight.X, bottomRight.Y),
                color,
                0f,
                ImDrawFlags.None,
                2f);
        }
    }

    private bool DrawAndHandleGizmo(
        ImDrawListPtr drawList,
        SceneDocument document,
        Camera2D camera,
        Vector2 canvasPosition,
        Vector2 mouseLocal,
        bool hovered)
    {
        if (hovered && _gizmoDrag is null)
        {
            if (ImGui.IsKeyPressed(ImGuiKey.Q)) _workspace.GizmoMode = 0;
            if (ImGui.IsKeyPressed(ImGuiKey.W)) _workspace.GizmoMode = 1;
            if (ImGui.IsKeyPressed(ImGuiKey.E)) _workspace.GizmoMode = 2;
            if (ImGui.IsKeyPressed(ImGuiKey.R)) _workspace.GizmoMode = 3;
        }

        var active = _selection.GetActive(document.Scene);
        if (active is null || _workspace.GizmoMode == 0)
            return false;
        if (document.TryGetBlueprintInstanceRoot(active, out var activeInstanceRoot, out _) &&
            !ReferenceEquals(active, activeInstanceRoot))
            return false;

        var screen = camera.WorldToScreen(active.Transform.WorldPosition2D);
        var center = canvasPosition + new Vector2(screen.X, screen.Y);
        var mouseScreen = canvasPosition + mouseLocal;
        var red = ImGui.GetColorU32(new Vector4(0.95f, 0.28f, 0.30f, 1f));
        var green = ImGui.GetColorU32(new Vector4(0.32f, 0.86f, 0.42f, 1f));
        var yellow = ImGui.GetColorU32(new Vector4(1f, 0.78f, 0.22f, 1f));
        var cyan = ImGui.GetColorU32(new Vector4(0.25f, 0.72f, 1f, 1f));
        var hit = false;

        switch (_workspace.GizmoMode)
        {
            case 1:
            {
                var xEnd = center + new Vector2(54f, 0f);
                var yEnd = center - new Vector2(0f, 54f);
                drawList.AddLine(center, xEnd, red, 3f);
                drawList.AddTriangleFilled(xEnd, xEnd - new Vector2(9f, 5f), xEnd - new Vector2(9f, -5f), red);
                drawList.AddLine(center, yEnd, green, 3f);
                drawList.AddTriangleFilled(yEnd, yEnd + new Vector2(-5f, 9f), yEnd + new Vector2(5f, 9f), green);
                drawList.AddRectFilled(center - new Vector2(5f), center + new Vector2(5f), cyan);
                hit = DistanceToSegment(mouseScreen, center, xEnd) <= 8f ||
                      DistanceToSegment(mouseScreen, center, yEnd) <= 8f;
                break;
            }
            case 2:
            {
                drawList.AddCircle(center, 46f, yellow, 64, 3f);
                hit = MathF.Abs(Vector2.Distance(mouseScreen, center) - 46f) <= 8f;
                break;
            }
            case 3:
            {
                var end = center + new Vector2(42f, -42f);
                drawList.AddLine(center, end, cyan, 3f);
                drawList.AddRectFilled(end - new Vector2(6f), end + new Vector2(6f), cyan);
                hit = Vector2.Distance(mouseScreen, end) <= 11f ||
                      DistanceToSegment(mouseScreen, center, end) <= 7f;
                break;
            }
        }

        if (_gizmoDrag is null && hovered && hit && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            var startWorld = camera.ScreenToWorld(new XnaVector2(mouseLocal.X, mouseLocal.Y));
            var states = _selection.Resolve(document.Scene)
                .Where(entity =>
                    !document.TryGetBlueprintInstanceRoot(entity, out var instanceRoot, out _) ||
                    ReferenceEquals(entity, instanceRoot))
                .Select(entity => new GizmoEntityStart(
                    entity.Id,
                    entity.Transform.WorldPosition,
                    entity.Transform.WorldRotation2D,
                    entity.Transform.Scale))
                .ToArray();
            var pivot = active.Transform.WorldPosition2D;
            _gizmoDrag = new GizmoDrag(
                _workspace.GizmoMode,
                document.BeginTransaction(_workspace.GizmoMode switch
                {
                    1 => "Move Entities",
                    2 => "Rotate Entities",
                    _ => "Scale Entities"
                }),
                startWorld,
                mouseLocal,
                pivot,
                MathF.Atan2(startWorld.Y - pivot.Y, startWorld.X - pivot.X),
                MathF.Max(0.001f, XnaVector2.Distance(startWorld, pivot)),
                states);
        }

        if (_gizmoDrag is null)
            return hit;

        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            _gizmoDrag.Transaction.Commit();
            _gizmoDrag = null;
            return true;
        }

        try
        {
            UpdateGizmoDrag(document, camera, mouseLocal);
        }
        catch
        {
            _gizmoDrag.Transaction.Cancel();
            _gizmoDrag = null;
            throw;
        }
        return true;
    }

    private void UpdateGizmoDrag(SceneDocument document, Camera2D camera, Vector2 mouseLocal)
    {
        var drag = _gizmoDrag!;
        var world = camera.ScreenToWorld(new XnaVector2(mouseLocal.X, mouseLocal.Y));
        drag.Transaction.Update(scene =>
        {
            switch (drag.Mode)
            {
                case 1:
                {
                    var delta = world - drag.StartWorld;
                    if (_workspace.SnapEnabled)
                    {
                        var snap = MathF.Max(0.001f, _workspace.MoveSnap);
                        delta.X = MathF.Round(delta.X / snap) * snap;
                        delta.Y = MathF.Round(delta.Y / snap) * snap;
                    }
                    foreach (var state in drag.Entities)
                        if (scene.FindEntity(state.Id) is { } entity)
                        {
                            entity.Transform.WorldPosition = state.WorldPosition + new Microsoft.Xna.Framework.Vector3(delta, 0f);
                            document.RecordLDtkPosition(entity);
                        }
                    break;
                }
                case 2:
                {
                    var angle = MathF.Atan2(world.Y - drag.Pivot.Y, world.X - drag.Pivot.X) - drag.StartAngle;
                    if (_workspace.SnapEnabled)
                    {
                        var snap = MathHelper.ToRadians(MathF.Max(0.1f, _workspace.RotateSnapDegrees));
                        angle = MathF.Round(angle / snap) * snap;
                    }
                    foreach (var state in drag.Entities)
                        if (scene.FindEntity(state.Id) is { } entity)
                        {
                            entity.Transform.WorldRotation2D = state.WorldRotation + angle;
                            document.RecordLDtkRotation(entity);
                        }
                    break;
                }
                case 3:
                {
                    var factor = XnaVector2.Distance(world, drag.Pivot) / drag.StartDistance;
                    if (_workspace.SnapEnabled)
                    {
                        var snap = MathF.Max(0.001f, _workspace.ScaleSnap);
                        factor = MathF.Round(factor / snap) * snap;
                    }
                    factor = MathF.Max(0.001f, factor);
                    foreach (var state in drag.Entities)
                        if (scene.FindEntity(state.Id) is { } entity)
                        {
                            entity.Transform.Scale = state.Scale * factor;
                            document.RecordLDtkScale(entity);
                        }
                    break;
                }
            }
        });
    }

    private bool DrawComponentHandles(
        ImDrawListPtr drawList,
        SceneDocument document,
        Camera2D editorCamera,
        Vector2 canvasPosition,
        Vector2 mouseLocal,
        bool hovered)
    {
        var colliderColor = ImGui.GetColorU32(new Vector4(0.32f, 0.92f, 0.55f, 0.9f));
        var cameraColor = ImGui.GetColorU32(new Vector4(0.65f, 0.42f, 1f, 0.9f));
        var pointLightColor = ImGui.GetColorU32(new Vector4(1f, 0.77f, 0.25f, 0.95f));
        foreach (var entity in _selection.Resolve(_documents.Current?.Scene))
        {
            if (document.TryGetBlueprintInstanceRoot(entity, out _, out _))
                continue;
            foreach (var collider in entity.GetAllComponents().OfType<Collider>())
            {
                if (collider.Bounds is null)
                    continue;
                try
                {
                    var vertices = collider.WorldPolygon2D.Vertices;
                    for (var index = 0; index < vertices.Length; index++)
                    {
                        var next = (index + 1) % vertices.Length;
                        var from = editorCamera.WorldToScreen(vertices[index]);
                        var to = editorCamera.WorldToScreen(vertices[next]);
                        drawList.AddLine(
                            canvasPosition + new Vector2(from.X, from.Y),
                            canvasPosition + new Vector2(to.X, to.Y),
                            colliderColor,
                            2f);
                    }

                    if (collider is BoxCollider box && box.Bounds is Box2D localBox)
                    {
                        for (var index = 0; index < vertices.Length; index++)
                        {
                            var vertexScreen = editorCamera.WorldToScreen(vertices[index]);
                            var handle = canvasPosition + new Vector2(vertexScreen.X, vertexScreen.Y);
                            drawList.AddRectFilled(handle - new Vector2(4), handle + new Vector2(4), colliderColor);
                            if (_colliderDrag is null && _pointLightDrag is null && hovered &&
                                Vector2.Distance(canvasPosition + mouseLocal, handle) <= 9f &&
                                ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                            {
                                _colliderDrag = new ColliderDrag(
                                    entity.Id,
                                    index,
                                    index switch
                                    {
                                        0 => localBox.BottomRight,
                                        1 => localBox.BottomLeft,
                                        2 => localBox.TopLeft,
                                        _ => localBox.TopRight
                                    },
                                    document.BeginTransaction("Resize Box Collider"));
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            foreach (var light in entity.GetAllComponents().OfType<PointLight2D>())
            {
                var handleWorld = light.Position + XnaVector2.UnitX * MathF.Max(0f, light.Radius);
                var handleScreen = editorCamera.WorldToScreen(handleWorld);
                var handle = canvasPosition + new Vector2(handleScreen.X, handleScreen.Y);
                drawList.AddCircleFilled(handle, 6f, pointLightColor, 20);
                drawList.AddCircle(handle, 9f, pointLightColor, 24, 1.5f);

                if (_colliderDrag is null && _pointLightDrag is null && hovered &&
                    Vector2.Distance(canvasPosition + mouseLocal, handle) <= 12f &&
                    ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    _pointLightDrag = new PointLightDrag(
                        entity.Id,
                        document.BeginTransaction("Resize Point Light"));
                }
            }
            foreach (var camera in entity.GetAllComponents().OfType<Camera2D>())
            {
                if (ReferenceEquals(camera, editorCamera))
                    continue;
                try
                {
                    var bounds = camera.BoundsF;
                    var topLeft = editorCamera.WorldToScreen(new XnaVector2(bounds.Left, bounds.Top));
                    var bottomRight = editorCamera.WorldToScreen(new XnaVector2(bounds.Right, bounds.Bottom));
                    drawList.AddRect(
                        canvasPosition + new Vector2(topLeft.X, topLeft.Y),
                        canvasPosition + new Vector2(bottomRight.X, bottomRight.Y),
                        cameraColor,
                        0,
                        ImDrawFlags.None,
                        2f);
                }
                catch
                {
                }
            }
        }

        if (_pointLightDrag is not null)
        {
            if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                _pointLightDrag.Transaction.Commit();
                _pointLightDrag = null;
                return true;
            }

            var pointLightWorld = editorCamera.ScreenToWorld(new XnaVector2(mouseLocal.X, mouseLocal.Y));
            var pointLightDrag = _pointLightDrag;
            pointLightDrag.Transaction.Update(scene =>
            {
                var entity = scene.FindEntity(pointLightDrag.EntityId);
                if (entity?.GetComponent<PointLight2D>() is not { } light)
                    return;
                light.Radius = CalculatePointLightRadius(
                    light.Position,
                    pointLightWorld,
                    _workspace.SnapEnabled,
                    _workspace.MoveSnap);
            });
            return true;
        }

        if (_colliderDrag is null)
            return false;
        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            _colliderDrag.Transaction.Commit();
            _colliderDrag = null;
            return true;
        }

        var world = editorCamera.ScreenToWorld(new XnaVector2(mouseLocal.X, mouseLocal.Y));
        var drag = _colliderDrag;
        drag.Transaction.Update(scene =>
        {
            var entity = scene.FindEntity(drag.EntityId);
            if (entity?.GetComponent<BoxCollider>() is not { } collider)
                return;
            var local = entity.Transform.InverseTransformPoint2D(world);
            if (_workspace.SnapEnabled)
            {
                var snap = MathF.Max(0.001f, _workspace.MoveSnap);
                local.X = MathF.Round(local.X / snap) * snap;
                local.Y = MathF.Round(local.Y / snap) * snap;
            }
            var center = (local + drag.OppositeLocal) * 0.5f;
            var halfWidth = MathF.Max(0.001f, MathF.Abs(local.X - drag.OppositeLocal.X) * 0.5f);
            var halfHeight = MathF.Max(0.001f, MathF.Abs(local.Y - drag.OppositeLocal.Y) * 0.5f);
            collider.SetShape(Box2D.CreateRectangle(center, halfWidth, halfHeight));
        });
        return true;
    }

    internal static float CalculatePointLightRadius(
        XnaVector2 center,
        XnaVector2 handle,
        bool snapEnabled,
        float snapSize)
    {
        var radius = XnaVector2.Distance(center, handle);
        if (snapEnabled)
        {
            var snap = MathF.Max(0.001f, snapSize);
            radius = MathF.Round(radius / snap) * snap;
        }

        return MathF.Max(0f, radius);
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        var segment = end - start;
        var lengthSquared = segment.LengthSquared();
        if (lengthSquared <= float.Epsilon)
            return Vector2.Distance(point, start);
        var t = Math.Clamp(Vector2.Dot(point - start, segment) / lengthSquared, 0f, 1f);
        return Vector2.Distance(point, start + segment * t);
    }

    private static void DrawEmptyCanvas(Vector2 position, Vector2 size, string title, string detail)
    {
        ImGui.InvisibleButton("##SceneCanvas", size);
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(position, position + size, ImGui.GetColorU32(new Vector4(0.075f, 0.082f, 0.095f, 1f)));
        var center = position + size * 0.5f;
        var titleSize = ImGui.CalcTextSize(title);
        var detailSize = ImGui.CalcTextSize(detail);
        drawList.AddText(center - new Vector2(titleSize.X * 0.5f, 18f), ImGui.GetColorU32(new Vector4(0.82f, 0.84f, 0.88f, 1f)), title);
        drawList.AddText(center - new Vector2(detailSize.X * 0.5f, -6f), ImGui.GetColorU32(new Vector4(0.50f, 0.53f, 0.59f, 1f)), detail);
    }

    protected override void DisposeCore() => _renderer.Dispose();

    private sealed record GizmoEntityStart(
        Guid Id,
        Microsoft.Xna.Framework.Vector3 WorldPosition,
        float WorldRotation,
        Microsoft.Xna.Framework.Vector3 Scale);

    private sealed record GizmoDrag(
        int Mode,
        SceneDocument.SceneEditTransaction Transaction,
        XnaVector2 StartWorld,
        Vector2 StartMouse,
        XnaVector2 Pivot,
        float StartAngle,
        float StartDistance,
        IReadOnlyList<GizmoEntityStart> Entities);

    private sealed record ColliderDrag(
        Guid EntityId,
        int Corner,
        XnaVector2 OppositeLocal,
        SceneDocument.SceneEditTransaction Transaction);

    private sealed record PointLightDrag(
        Guid EntityId,
        SceneDocument.SceneEditTransaction Transaction);

    private sealed class ImGuiEditorGizmoContext(
        ImDrawListPtr drawList,
        Camera2D camera,
        Vector2 canvasPosition) : IEditorGizmoContext
    {
        public void Line(XnaVector2 from, XnaVector2 to, Color color, float thickness = 1f) =>
            drawList.AddLine(Screen(from), Screen(to), ColorU32(color), MathF.Max(1f, thickness));

        public void Circle(XnaVector2 center, float radius, Color color, float thickness = 1f) =>
            drawList.AddCircle(
                Screen(center),
                MathF.Abs(radius * camera.Scale),
                ColorU32(color),
                48,
                MathF.Max(1f, thickness));

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
