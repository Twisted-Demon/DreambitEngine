using System.Numerics;
using ImGuiNET;

namespace Dreambit.Editor.UI.Panels;

internal sealed class ScenePanel : EditorPanel
{
    private const float GridSize = 32f;

    public ScenePanel()
        : base(EditorPanelIds.Scene, "Scene")
    {
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

        ImGui.InvisibleButton(
            "##SceneCanvas",
            canvasSize,
            ImGuiButtonFlags.MouseButtonLeft |
            ImGuiButtonFlags.MouseButtonRight |
            ImGuiButtonFlags.MouseButtonMiddle);

        var drawList = ImGui.GetWindowDrawList();
        var canvasEnd = canvasPosition + canvasSize;
        drawList.AddRectFilled(
            canvasPosition,
            canvasEnd,
            ImGui.GetColorU32(new Vector4(0.075f, 0.082f, 0.095f, 1f)));

        var gridColor = ImGui.GetColorU32(new Vector4(0.17f, 0.18f, 0.21f, 0.55f));
        for (var x = canvasPosition.X; x < canvasEnd.X; x += GridSize)
            drawList.AddLine(new Vector2(x, canvasPosition.Y), new Vector2(x, canvasEnd.Y), gridColor);
        for (var y = canvasPosition.Y; y < canvasEnd.Y; y += GridSize)
            drawList.AddLine(new Vector2(canvasPosition.X, y), new Vector2(canvasEnd.X, y), gridColor);

        var message = "Scene rendering is not active yet";
        var detail = "The editor camera and Dreambit render target arrive in Milestone 7.";
        var messageSize = ImGui.CalcTextSize(message);
        var detailSize = ImGui.CalcTextSize(detail);
        var center = canvasPosition + canvasSize * 0.5f;

        drawList.AddText(
            center - new Vector2(messageSize.X * 0.5f, 18f),
            ImGui.GetColorU32(new Vector4(0.82f, 0.84f, 0.88f, 1f)),
            message);
        drawList.AddText(
            center - new Vector2(detailSize.X * 0.5f, -6f),
            ImGui.GetColorU32(new Vector4(0.50f, 0.53f, 0.59f, 1f)),
            detail);
    }

    private static void DrawToolbar()
    {
        ImGui.BeginDisabled();
        ImGui.Button("Q", new Vector2(28f, 0f));
        ImGui.SameLine();
        ImGui.Button("W", new Vector2(28f, 0f));
        ImGui.SameLine();
        ImGui.Button("E", new Vector2(28f, 0f));
        ImGui.SameLine();
        ImGui.Button("R", new Vector2(28f, 0f));
        ImGui.SameLine();
        ImGui.TextDisabled("  Local   |   Grid 1");
        ImGui.EndDisabled();
    }
}
