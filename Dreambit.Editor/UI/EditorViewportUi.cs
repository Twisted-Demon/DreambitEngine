using System.Numerics;
using Dreambit.ECS;
using Dreambit.Editor.Persistence;
using ImGuiNET;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace Dreambit.Editor.UI;

internal static class EditorViewportUi
{
    public const float MinimumZoom = 0.02f;

    public static float NormalizeZoom(float zoom) =>
        float.IsFinite(zoom) && zoom >= MinimumZoom ? zoom : MinimumZoom;

    public static float ApplyZoomWheel(float zoom, float wheel)
    {
        var current = NormalizeZoom(zoom);
        var next = current * MathF.Pow(1.15f, wheel);
        return float.IsFinite(next) ? MathF.Max(MinimumZoom, next) : current;
    }

    public static float NormalizeGridSize(float gridSize) =>
        float.IsFinite(gridSize) ? MathF.Max(0.001f, gridSize) : 1f;

    public static void DrawSettingsPopup(string id, EditorWorkspaceState workspace)
    {
        if (!ImGui.BeginPopup(id))
            return;

        ImGui.TextUnformatted("Scene View");
        ImGui.Separator();

        var showGrid = workspace.ShowGrid;
        if (ImGui.Checkbox("Show Grid", ref showGrid))
            workspace.ShowGrid = showGrid;

        var gridSize = NormalizeGridSize(workspace.GridSize);
        ImGui.SetNextItemWidth(150f);
        if (ImGui.DragFloat("Grid Size", ref gridSize, 0.05f, 0.001f, 0f, "%.3f"))
            workspace.GridSize = NormalizeGridSize(gridSize);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("World units between grid lines. Ctrl+click to type an exact value.");

        ImGui.Spacing();
        var snap = workspace.SnapEnabled;
        if (ImGui.Checkbox("Enable Snapping", ref snap))
            workspace.SnapEnabled = snap;

        ImGui.BeginDisabled(!workspace.SnapEnabled);
        var moveSnap = workspace.MoveSnap;
        if (DrawPositiveSetting("Move Step", ref moveSnap, 0.05f, 0.001f, "%.3f"))
            workspace.MoveSnap = moveSnap;
        var rotateSnap = workspace.RotateSnapDegrees;
        if (DrawPositiveSetting("Rotation Step", ref rotateSnap, 1f, 0.1f, "%.1f deg"))
            workspace.RotateSnapDegrees = rotateSnap;
        var scaleSnap = workspace.ScaleSnap;
        if (DrawPositiveSetting("Scale Step", ref scaleSnap, 0.01f, 0.001f, "%.3f"))
            workspace.ScaleSnap = scaleSnap;
        ImGui.EndDisabled();

        if (ImGui.Button("Reset View Settings"))
        {
            workspace.ShowGrid = true;
            workspace.GridSize = 1f;
            workspace.SnapEnabled = false;
            workspace.MoveSnap = 1f;
            workspace.RotateSnapDegrees = 15f;
            workspace.ScaleSnap = 0.1f;
        }

        ImGui.EndPopup();
    }

    public static void DrawGrid(
        ImDrawListPtr drawList,
        Camera2D camera,
        Vector2 canvasPosition,
        Vector2 canvasSize,
        float configuredGridSize)
    {
        var bounds = camera.BoundsF;
        var step = NormalizeGridSize(configuredGridSize);
        var pixelsPerStep = MathF.Abs(step * camera.Scale);
        while (float.IsFinite(pixelsPerStep) && pixelsPerStep < 12f && step < 1e30f)
        {
            step *= 10f;
            pixelsPerStep *= 10f;
        }

        if (!float.IsFinite(step) || step <= 0f ||
            !float.IsFinite(bounds.Left) || !float.IsFinite(bounds.Right) ||
            !float.IsFinite(bounds.Top) || !float.IsFinite(bounds.Bottom))
        {
            return;
        }

        var minimumX = MathF.Floor(bounds.Left / step) * step;
        var minimumY = MathF.Floor(bounds.Top / step) * step;
        var color = ImGui.GetColorU32(new Vector4(0.72f, 0.76f, 0.84f, 0.10f));
        var axisColor = ImGui.GetColorU32(new Vector4(0.34f, 0.68f, 1f, 0.34f));

        var lineCount = 0;
        for (var worldX = minimumX;
             worldX <= bounds.Right && lineCount++ < 4096;
             worldX += step)
        {
            var screen = camera.WorldToScreen(new XnaVector2(worldX, 0));
            var x = canvasPosition.X + screen.X;
            if (x < canvasPosition.X || x > canvasPosition.X + canvasSize.X)
                continue;
            drawList.AddLine(
                new Vector2(x, canvasPosition.Y),
                new Vector2(x, canvasPosition.Y + canvasSize.Y),
                MathF.Abs(worldX) < step * 0.001f ? axisColor : color);
        }

        lineCount = 0;
        for (var worldY = minimumY;
             worldY <= bounds.Bottom && lineCount++ < 4096;
             worldY += step)
        {
            var screen = camera.WorldToScreen(new XnaVector2(0, worldY));
            var y = canvasPosition.Y + screen.Y;
            if (y < canvasPosition.Y || y > canvasPosition.Y + canvasSize.Y)
                continue;
            drawList.AddLine(
                new Vector2(canvasPosition.X, y),
                new Vector2(canvasPosition.X + canvasSize.X, y),
                MathF.Abs(worldY) < step * 0.001f ? axisColor : color);
        }
    }

    private static bool DrawPositiveSetting(
        string label,
        ref float value,
        float speed,
        float minimum,
        string format)
    {
        var edited = float.IsFinite(value) ? MathF.Max(minimum, value) : minimum;
        ImGui.SetNextItemWidth(150f);
        if (!ImGui.DragFloat(label, ref edited, speed, minimum, 0f, format))
            return false;
        value = float.IsFinite(edited) ? MathF.Max(minimum, edited) : minimum;
        return true;
    }
}
