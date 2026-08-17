using Dreambit.ECS;
using Dreambit.EditorApi;
using ImGuiNET;
using Vector2 = System.Numerics.Vector2;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace Dreambit.Editor.UI.Viewport;

/// <summary>Draws the visible world bounds of authored cameras selected in a viewport.</summary>
internal sealed class Camera2DEditorGizmo : IEditorComponentGizmo
{
    public string DisplayName => "Camera gizmo";

    public void Draw(
        EditorComponentGizmoFrame frame,
        Entity entity,
        bool allowInteraction,
        ref IEditorComponentGizmoInteraction? startedInteraction)
    {
        if (entity.GetComponent<Camera2D>() is not { } camera ||
            ReferenceEquals(camera, frame.Camera))
        {
            return;
        }

        var bounds = camera.BoundsF;
        var topLeft = frame.Camera.WorldToScreen(new XnaVector2(bounds.Left, bounds.Top));
        var bottomRight = frame.Camera.WorldToScreen(new XnaVector2(bounds.Right, bounds.Bottom));
        frame.DrawList.AddRect(
            frame.CanvasPosition + new Vector2(topLeft.X, topLeft.Y),
            frame.CanvasPosition + new Vector2(bottomRight.X, bottomRight.Y),
            ImGui.GetColorU32(EditorGuiTheme.SecondaryAccent),
            0f,
            ImDrawFlags.None,
            2f);
    }
}
