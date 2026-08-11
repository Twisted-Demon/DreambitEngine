namespace Dreambit;

public class Tileset : DreambitAsset
{
    public string Identifier { get; set; } = string.Empty;
    public int Padding { get; set; }
    public int TileGridSize { get; set; }
    public SpriteSheet SpriteSheet { get; private set; }
    public Sprite Sprite { get; private set; }
    public int PixelsPerUnit { get; set; } = 1;

    private TextureAsset _texture;
    public TextureAsset Texture
    {
        get => _texture;
        set
        {
            _texture = value;
            if (_texture?.Texture is null)
            {
                Sprite = null;
                SpriteSheet = null;
                return;
            }

            Sprite = Sprite.Create(_texture.Texture, pixelsPerUnit: PixelsPerUnit);
            Sprite.TextureAsset = _texture;
            SpriteSheet = SpriteSheet.Create(TileGridSize, Sprite);
        }
    }
}
