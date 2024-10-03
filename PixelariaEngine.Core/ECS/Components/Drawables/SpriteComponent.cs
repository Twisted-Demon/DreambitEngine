using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine.ECS;

public class SpriteComponent : DrawableComponent
{
    private Texture2D Texture { get; set; }
    public string TexturePath { get; set; }
    
    public override void OnAddedToEntity()
    {
        Texture = Core.Instance.Content.Load<Texture2D>(TexturePath);
    }

    public override void OnDraw(SpriteBatch spriteBatch)
    {
        if (Texture == null) return;
        
        spriteBatch.Draw(Texture, Transform.Position, color: Color.White );
    }
}