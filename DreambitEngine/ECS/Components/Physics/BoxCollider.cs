using System;
using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

[BlueprintType(nameof(BoxCollider))]
public class BoxCollider : Collider
{
    public override void OnCreated()
    {
        base.OnCreated();
        EnsureBoxBounds();
    }

    public override void OnEditorCreated()
    {
        EnsureBoxBounds();
    }

    public void SetShape(Box2D shape)
    {
        Bounds = shape;
    }

    private void EnsureBoxBounds()
    {

        if (Bounds is Box2D)
            return;

        if (Bounds is null)
        {
            Bounds = Box2D.CreateSquare(Vector2.Zero, 5.0f);
            return;
        }

        var verts = Bounds.GetVertices();

        if (verts is not { Length: 4 })
        {
            throw new InvalidOperationException(
                $"{nameof(BoxCollider)} requires exactly four bounds vertices, " +
                $"but the deserialized shape contained {verts?.Length ?? 0}.");
        }

        Bounds = Box2D.CreateFromVerts(verts);
    }
}
