using Dreambit.ECS;
using Dreambit.Editor.Persistence;
using Dreambit.Editor.Scenes;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace Dreambit.Editor.UI;

/// <summary>
/// Shared selection overlay and transform interaction for editor-hosted scene documents.
/// SceneDocument transactions keep scene and Blueprint edits on the same undoable path.
/// </summary>
internal sealed class EditorTransformGizmo(
    SelectionService selection,
    EditorWorkspaceState workspace)
{
    private GizmoDrag? _drag;

    public void DrawSelection(
        ImDrawListPtr drawList,
        Scene scene,
        Camera2D camera,
        Vector2 canvasPosition)
    {
        var color = ImGui.GetColorU32(new Vector4(0.24f, 0.65f, 1f, 1f));
        foreach (var entity in selection.Resolve(scene))
        {
            RectangleF? bounds = null;
            foreach (var drawable in entity.GetAllComponents().OfType<DrawableComponent>())
            {
                try
                {
                    bounds = bounds is null
                        ? drawable.Bounds
                        : RectangleF.Union(bounds.Value, drawable.Bounds);
                }
                catch
                {
                    // Custom bounds are an extension boundary; still show the entity pivot.
                }
            }

            if (bounds is null)
            {
                var center = camera.WorldToScreen(entity.Transform.WorldPosition2D);
                drawList.AddCircle(
                    canvasPosition + new Vector2(center.X, center.Y),
                    6f,
                    color,
                    16,
                    2f);
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

    public bool DrawAndHandle(
        ImDrawListPtr drawList,
        SceneDocument document,
        Camera2D camera,
        Vector2 canvasPosition,
        Vector2 mouseLocal,
        bool hovered)
    {
        if (hovered && _drag is null)
        {
            if (ImGui.IsKeyPressed(ImGuiKey.Q)) workspace.GizmoMode = 0;
            if (ImGui.IsKeyPressed(ImGuiKey.W)) workspace.GizmoMode = 1;
            if (ImGui.IsKeyPressed(ImGuiKey.E)) workspace.GizmoMode = 2;
            if (ImGui.IsKeyPressed(ImGuiKey.R)) workspace.GizmoMode = 3;
        }

        var active = selection.GetActive(document.Scene);
        if (active is null || workspace.GizmoMode == 0)
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

        switch (workspace.GizmoMode)
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
                drawList.AddCircle(center, 46f, yellow, 64, 3f);
                hit = MathF.Abs(Vector2.Distance(mouseScreen, center) - 46f) <= 8f;
                break;
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

        if (_drag is null && hovered && hit && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            var startWorld = camera.ScreenToWorld(new XnaVector2(mouseLocal.X, mouseLocal.Y));
            var states = selection.Resolve(document.Scene)
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
            _drag = new GizmoDrag(
                workspace.GizmoMode,
                document.BeginTransaction(workspace.GizmoMode switch
                {
                    1 => "Move Entities",
                    2 => "Rotate Entities",
                    _ => "Scale Entities"
                }),
                startWorld,
                pivot,
                MathF.Atan2(startWorld.Y - pivot.Y, startWorld.X - pivot.X),
                MathF.Max(0.001f, XnaVector2.Distance(startWorld, pivot)),
                states);
        }

        if (_drag is null)
            return hit;
        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            _drag.Transaction.Commit();
            _drag = null;
            return true;
        }

        try
        {
            UpdateDrag(document, camera, mouseLocal);
        }
        catch
        {
            _drag.Transaction.Cancel();
            _drag = null;
            throw;
        }
        return true;
    }

    private void UpdateDrag(SceneDocument document, Camera2D camera, Vector2 mouseLocal)
    {
        var drag = _drag!;
        var world = camera.ScreenToWorld(new XnaVector2(mouseLocal.X, mouseLocal.Y));
        drag.Transaction.Update(scene =>
        {
            switch (drag.Mode)
            {
                case 1:
                {
                    var delta = world - drag.StartWorld;
                    if (workspace.SnapEnabled)
                    {
                        var snap = MathF.Max(0.001f, workspace.MoveSnap);
                        delta.X = MathF.Round(delta.X / snap) * snap;
                        delta.Y = MathF.Round(delta.Y / snap) * snap;
                    }
                    foreach (var state in drag.Entities)
                        if (scene.FindEntity(state.Id) is { } entity)
                        {
                            entity.Transform.WorldPosition = state.WorldPosition + new Vector3(delta, 0f);
                            document.RecordLDtkPosition(entity);
                        }
                    break;
                }
                case 2:
                {
                    var angle = MathF.Atan2(world.Y - drag.Pivot.Y, world.X - drag.Pivot.X) - drag.StartAngle;
                    if (workspace.SnapEnabled)
                    {
                        var snap = MathHelper.ToRadians(MathF.Max(0.1f, workspace.RotateSnapDegrees));
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
                    if (workspace.SnapEnabled)
                    {
                        var snap = MathF.Max(0.001f, workspace.ScaleSnap);
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

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        var segment = end - start;
        var lengthSquared = segment.LengthSquared();
        if (lengthSquared <= float.Epsilon)
            return Vector2.Distance(point, start);
        var t = Math.Clamp(Vector2.Dot(point - start, segment) / lengthSquared, 0f, 1f);
        return Vector2.Distance(point, start + segment * t);
    }

    private sealed record GizmoEntityStart(
        Guid Id,
        Vector3 WorldPosition,
        float WorldRotation,
        Vector3 Scale);

    private sealed record GizmoDrag(
        int Mode,
        SceneDocument.SceneEditTransaction Transaction,
        XnaVector2 StartWorld,
        XnaVector2 Pivot,
        float StartAngle,
        float StartDistance,
        IReadOnlyList<GizmoEntityStart> Entities);
}
