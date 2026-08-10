namespace Dreambit;

public class Tileset : DreambitAsset
{
    public string Identifier { get; set; } = string.Empty;
    public int Padding { get; set; }
    public int TileGridSize { get; set; }
    public SpriteSheet SpriteSheet { get; private set; }
    public Sprite Sprite { get; private set; }
    public int PixelsPerUnit { get; set; } = 1;

    private string _texturePath;
    public string TexturePath
    {
        get => _texturePath;
        set
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            _texturePath = value;

            Sprite = Sprite.Create(_texturePath, pixelsPerUnit: PixelsPerUnit);
            SpriteSheet = SpriteSheet.Create(TileGridSize, Sprite);
        }
    }


}
