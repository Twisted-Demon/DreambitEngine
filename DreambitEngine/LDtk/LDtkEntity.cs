using System.Collections.Generic;
using System.Linq;
using Dreambit.ECS;
using LDtk;
using Microsoft.Xna.Framework;

namespace Dreambit;

public class LDtkEntity<T> : LDtkEntity where T : new()
{
    protected Scene Scene = Core.Instance.CurrentScene;

    protected Entity CreateEntity<TU>(TU data, string name = null, HashSet<string> tags = null) where TU : ILDtkEntity
    {
        name ??= data.Identifier;

        var entity = Entity.Create(name, tags, createAt: new Vector3(data.Position, 0), guidOverride: data.Iid);
        entity.AttachComponent<LDtkIid>().Iid = data.Iid;

        return entity;
    }

    protected void AttachTileSpriteDrawer<TU>(TU data, Entity entity, TilesetRectangle tilesetRect, Color? color = null)
        where TU : ILDtkEntity
    {
        if (tilesetRect == null) return;

        var drawer = entity.AttachComponent<SpriteDrawer>();

        drawer.WithPivot(new Vector2(data.Pivot.X * tilesetRect.W, data.Pivot.Y * tilesetRect.H));
        drawer.WithPivot(PivotType.Custom);
        drawer.WithTint(color ?? data.SmartColor);

        var sprite = new Sprite
        {
            Texture = LDtkManager.Instance.SpriteSheets[tilesetRect.TilesetUid].Texture,
            SourceRect = tilesetRect
        };
        drawer.SetSprite(sprite);
    }

    protected PolyShapeCollider CreatePolyCollider(Entity entity, Point[] points, Vector2 entityPosition)
    {
        var verts = points.Select(p => new Vector2(p.X, p.Y) - entityPosition).ToArray();

        var bounds = entity.AttachComponent<PolyShapeCollider>();
        var shape = PolyShape2D.Create(verts);
        bounds.SetShape(shape);
        return bounds;
    }

    /// <summary>
    ///     Override this to create the object
    /// </summary>
    /// <param name="level"></param>
    protected virtual void SetUp(LDtkLevel level)
    {
    }

    public static void SetUpEntities(LDtkLevel level)
    {
        var ldtkEntities = level.GetEntities<T>();

        foreach (var ldtkEntity in ldtkEntities)
        {
            var entity = ldtkEntity as LDtkEntity<T>;

            entity?.SetUp(level);
        }
    }
}

public class LDtkEntity
{
}