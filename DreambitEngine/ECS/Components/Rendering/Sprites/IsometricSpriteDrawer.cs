using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public class IsometricSpriteDrawer
    : SpriteDrawer
{
    public Vector2 RenderOffset { get; set; } = Vector2.Zero;

    public float SortOffset { get; set; }

    public bool ProjectWorldRotation { get; set; } = true;

    public float RotationOffset { get; set; }

    public Vector2 ProjectedPosition =>
        IsometricProjection.WorldToRender(
            Transform.WorldPosition) +
        RenderOffset;

    public override float SortDepth =>
        IsometricProjection.GetSortDepth(Transform.WorldPosition) + SortOffset;

    protected override Vector2 GetDrawPosition()
    {
        return ProjectedPosition;
    }

    protected override float GetDrawRotation()
    {
        if (!ProjectWorldRotation)
            return RotationOffset;

        return
            IsometricProjection
                .WorldDirectionToRenderRotation(
                    Transform.Forward2D) +
            RotationOffset;
    }
}
