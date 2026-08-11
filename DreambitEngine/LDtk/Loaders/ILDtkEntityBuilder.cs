using Dreambit.ECS;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.LDtk.Loaders;

public interface ILDtkEntityBuilder
{
    string[] EntityDefinitionIdentifiers { get; }

    Entity BuildEntity(
        LDtkScene scene,
        LDtkLevelInstance level,
        LDtkEntity ldtkEntity);
}

public abstract class LDtkEntityBuilder : ILDtkEntityBuilder
{
    protected ILogger Logger => new Logger(GetType());
    public abstract string[] EntityDefinitionIdentifiers { get; }
    public abstract Entity BuildEntity(
        LDtkScene scene,
        LDtkLevelInstance level,
        LDtkEntity ldtkEntity);

    protected void SetTileSheetSprite(Entity entity, LDtkEntity ldtkEntity)
    {
        var spriteDrawer = entity.GetComponent<SpriteDrawer>();

        if (ldtkEntity.Instance._Tile is null) return;
        var tilesetUid = ldtkEntity.Instance._Tile.TilesetUid;

        var tileset = LDtkManager.Instance.LDtkProject.GetTileset(tilesetUid);
        var textureSource = tileset.SourcePath;

        if(textureSource is null) return;
        textureSource = textureSource.Replace(".png", "");

        var texture = Resources.LoadAsset<Texture2D>(textureSource);

        if(texture is null) return;
        var sprite = Sprite.Create(texture, ldtkEntity.Tile, 16f);

        var pivot = ldtkEntity.Tile.NormalizedPivot(ldtkEntity.Pivot);

        spriteDrawer.SetSprite(sprite);
        spriteDrawer.WithPivot(pivot);
    }
}
