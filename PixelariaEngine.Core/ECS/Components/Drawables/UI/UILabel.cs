using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelariaEngine.Graphics;

namespace PixelariaEngine.ECS;

public class UILabel : UIElement
{
    private string _texturePath = string.Empty;

    public string TexturePath
    {
        get => _texturePath;
        set
        {
            if(_texturePath == value) return;
            _texturePath = value;
            SpriteSheet = SpriteSheet.Create(3, 3, value);
        }
    }
    
    public SpriteSheet SpriteSheet { get; set; }
    
    public PivotType LabelPivot { get; set; } = PivotType.Center;
    public UIText TextElement { get; set; }
    
    public Vector2 Size { get; set; }

    public string Text
    {
        get => TextElement.Text;
        set => TextElement.Text = value;
    }

    public override void OnDrawUI()
    {
        var position = GetScreenPos();
    }

    public static UILabel Create(Canvas canvas, string text, string fontName, string texturePath = null)
    {
        var uiLabel = canvas.CreateUIElement<UILabel>();
        uiLabel.TextElement = uiLabel.Entity.AttachComponent<UIText>();
        
        uiLabel.TextElement.FontName = fontName;
        uiLabel.TextElement.Text = text;
        
        if(texturePath != null)
            uiLabel.TexturePath = texturePath;
        
        return uiLabel;
    }
}