using Dreambit.ECS;
using Dreambit.Editor.Scenes;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace Dreambit.Editor.UI.Viewport;

/// <summary>Draws selection bounds independently from transform manipulation policy.</summary>
internal sealed class EditorSelectionOverlay
{
    public string? LastError { get; private set; }

    public void Draw(
        ImDrawListPtr drawList,
        SceneDocument document,
        Camera2D camera,
        Vector2 canvasPosition,
        Func<Entity, RectangleF?> resolveBounds)
    {
        LastError = null;
        var color = ImGui.GetColorU32(new Vector4(0.24f, 0.65f, 1f, 1f));
        foreach (var entity in document.Selection.Resolve(document.Scene))
        {
            RectangleF? bounds;
            try
            {
                bounds = resolveBounds(entity);
            }
            catch (Exception exception)
            {
                LastError ??= $"Could not read selection bounds: {exception.Message}";
                bounds = null;
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

            var topLeft = camera.WorldToScreen(
                new XnaVector2(bounds.Value.Left, bounds.Value.Top));
            var bottomRight = camera.WorldToScreen(
                new XnaVector2(bounds.Value.Right, bounds.Value.Bottom));
            drawList.AddRect(
                canvasPosition + new Vector2(topLeft.X, topLeft.Y),
                canvasPosition + new Vector2(bottomRight.X, bottomRight.Y),
                color,
                0f,
                ImDrawFlags.None,
                2f);
        }
    }

    public RectangleF? GetEntityDrawableBounds(Entity entity)
    {
        RectangleF? bounds = null;
        foreach (var drawable in entity.GetAllComponents().OfType<DrawableComponent>())
        {
            try
            {
                var drawableBounds = drawable.Bounds;
                bounds = bounds is null
                    ? drawableBounds
                    : RectangleF.Union(bounds.Value, drawableBounds);
            }
            catch (Exception exception)
            {
                LastError ??=
                    $"{drawable.GetType().FullName ?? drawable.GetType().Name} could not provide editor bounds: " +
                    exception.Message;
            }
        }
        return bounds;
    }

    public RectangleF? GetHierarchyDrawableBounds(Entity root)
    {
        var bounds = GetEntityDrawableBounds(root);
        foreach (var child in root.Children)
        {
            var childBounds = GetHierarchyDrawableBounds(child);
            if (childBounds is null)
                continue;
            bounds = bounds is null
                ? childBounds
                : RectangleF.Union(bounds.Value, childBounds.Value);
        }
        return bounds;
    }
}
