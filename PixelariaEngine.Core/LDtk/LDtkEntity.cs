using LDtk;
using Microsoft.Xna.Framework;
using PixelariaEngine.ECS;
using PixelariaEngine.Graphics;

namespace PixelariaEngine;

public class LDtkEntity<T> : LDtkEntity where T : new()
{
    protected Entity CreateEntity<TU>(TU data) where TU : ILDtkEntity
    {
        var entity = Entity.Create(data.Identifier);
        entity.Transform.Position = new Vector3(data.Position, 0);
        entity.AttachComponent<LDtkEntityComponent>().Iid = data.Iid;

        return entity;
    }

    protected void AttachTileSpriteDrawer<TU>(TU data, Entity entity, TilesetRectangle tilesetRect, Color? color = null)
        where TU : ILDtkEntity
    {
        if (tilesetRect == null) return;

        var sprite = entity.AttachComponent<SpriteDrawer>();

        sprite.Pivot = new Vector2(data.Pivot.X * tilesetRect.W, data.Pivot.Y * tilesetRect.H);
        sprite.PivotType = SpritePivot.Custom;
        sprite.Color = color ?? data.SmartColor;

        sprite.SpriteSheet = LDtkManager.Instance.SpriteSheets[tilesetRect.TilesetUid];
        sprite.FrameRect = tilesetRect;
    }

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