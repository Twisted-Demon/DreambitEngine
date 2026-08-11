using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

/// <summary>Renderer-neutral world-space primitives available to editor component gizmos.</summary>
public interface IEditorGizmoContext
{
    void Line(Vector2 from, Vector2 to, Color color, float thickness = 1f);
    void Circle(Vector2 center, float radius, Color color, float thickness = 1f);
    void Rectangle(RectangleF rectangle, Color color, float thickness = 1f);
    void Label(Vector2 position, string text, Color color);
}
