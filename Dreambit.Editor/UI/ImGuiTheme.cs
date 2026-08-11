using System.Numerics;
using ImGuiNET;

namespace Dreambit.Editor.UI;

internal static class ImGuiTheme
{
    public static void Apply()
    {
        ImGui.StyleColorsDark();
        var style = ImGui.GetStyle();

        style.WindowPadding = new Vector2(10f, 9f);
        style.FramePadding = new Vector2(8f, 5f);
        style.CellPadding = new Vector2(7f, 5f);
        style.ItemSpacing = new Vector2(8f, 6f);
        style.ItemInnerSpacing = new Vector2(6f, 4f);
        style.ScrollbarSize = 13f;
        style.GrabMinSize = 9f;
        style.WindowBorderSize = 1f;
        style.ChildBorderSize = 1f;
        style.PopupBorderSize = 1f;
        style.FrameBorderSize = 0f;
        style.TabBorderSize = 0f;
        style.WindowRounding = 4f;
        style.ChildRounding = 3f;
        style.FrameRounding = 3f;
        style.PopupRounding = 4f;
        style.ScrollbarRounding = 9f;
        style.GrabRounding = 3f;
        style.TabRounding = 3f;

        var colors = style.Colors;
        colors[(int)ImGuiCol.Text] = new Vector4(0.86f, 0.88f, 0.91f, 1f);
        colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.49f, 0.52f, 0.58f, 1f);
        colors[(int)ImGuiCol.WindowBg] = new Vector4(0.080f, 0.086f, 0.098f, 1f);
        colors[(int)ImGuiCol.ChildBg] = new Vector4(0.080f, 0.086f, 0.098f, 1f);
        colors[(int)ImGuiCol.PopupBg] = new Vector4(0.105f, 0.113f, 0.129f, 0.98f);
        colors[(int)ImGuiCol.Border] = new Vector4(0.20f, 0.22f, 0.26f, 0.80f);
        colors[(int)ImGuiCol.FrameBg] = new Vector4(0.135f, 0.146f, 0.169f, 1f);
        colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.19f, 0.22f, 0.27f, 1f);
        colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.23f, 0.28f, 0.35f, 1f);
        colors[(int)ImGuiCol.TitleBg] = new Vector4(0.075f, 0.082f, 0.095f, 1f);
        colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.105f, 0.118f, 0.142f, 1f);
        colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.070f, 0.076f, 0.088f, 1f);
        colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.065f, 0.071f, 0.082f, 1f);
        colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.22f, 0.24f, 0.29f, 1f);
        colors[(int)ImGuiCol.CheckMark] = new Vector4(0.30f, 0.66f, 0.98f, 1f);
        colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.30f, 0.66f, 0.98f, 1f);
        colors[(int)ImGuiCol.Button] = new Vector4(0.16f, 0.18f, 0.22f, 1f);
        colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.22f, 0.39f, 0.58f, 1f);
        colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.25f, 0.50f, 0.75f, 1f);
        colors[(int)ImGuiCol.Header] = new Vector4(0.18f, 0.25f, 0.34f, 1f);
        colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.22f, 0.39f, 0.58f, 1f);
        colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.25f, 0.50f, 0.75f, 1f);
        colors[(int)ImGuiCol.Separator] = new Vector4(0.18f, 0.20f, 0.24f, 1f);
        colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.30f, 0.66f, 0.98f, 0.22f);
        colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.30f, 0.66f, 0.98f, 0.67f);
        colors[(int)ImGuiCol.Tab] = new Vector4(0.11f, 0.12f, 0.14f, 1f);
        colors[(int)ImGuiCol.TabHovered] = new Vector4(0.22f, 0.39f, 0.58f, 1f);
        colors[(int)ImGuiCol.TabSelected] = new Vector4(0.16f, 0.24f, 0.33f, 1f);
        colors[(int)ImGuiCol.DockingPreview] = new Vector4(0.30f, 0.66f, 0.98f, 0.70f);
        colors[(int)ImGuiCol.DockingEmptyBg] = new Vector4(0.065f, 0.071f, 0.082f, 1f);
    }
}
