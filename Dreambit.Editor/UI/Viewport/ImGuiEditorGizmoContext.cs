using Dreambit.ECS;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace Dreambit.Editor.UI.Viewport;

/// <summary>Adapts runtime-neutral editor drawing primitives to the active ImGui viewport.</summary>
internal sealed class ImGuiEditorGizmoContext(
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
