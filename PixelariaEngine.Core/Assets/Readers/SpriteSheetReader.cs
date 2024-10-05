using Microsoft.Xna.Framework.Content;

namespace PixelariaEngine;

public class SpriteSheetReader : ContentTypeReader<SpriteSheet>
{
    protected override SpriteSheet Read(ContentReader input, SpriteSheet existingInstance)
    {
        var texturePath = input.ReadString();
        var columns = input.ReadInt32();
        var rows = input.ReadInt32();

        var spriteSheet = SpriteSheet.Create(columns, rows, texturePath);
        spriteSheet.AssetName = input.AssetName;

        return spriteSheet;
    }
}