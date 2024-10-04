using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine.ECS;

public class SpriteComponent : DrawableComponent
{
    private Texture2D Texture { get; set; }
    private string _texturePath = string.Empty;
    
    public string TexturePath
    {
        get => _texturePath;
        set
        {
            if(_texturePath == value) return;
            _texturePath = value;
            Texture = Core.Instance.Content.Load<Texture2D>(_texturePath);
            OnTextureChanged();
        }
    }
    public Vector2 Origin { get; set; }

    private void OnTextureChanged()
    {
        Origin = new Vector2((float)Texture.Width / 2, (float)Texture.Height / 2);
    }
    
    public override void OnDraw()
    {
        if (Texture == null) return;
        
        Core.SpriteBatch.Draw(
            Texture,
            new Vector2(Transform.Position.X, Transform.Position.Y),
            null,
            Color.White,
            Transform.Rotation.Z,
            Origin,
            new Vector2(Transform.Scale.X, Transform.Scale.Y),
            SpriteEffects.None,
            0f
            );
    }
}