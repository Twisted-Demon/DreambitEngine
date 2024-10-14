using Microsoft.Xna.Framework;
using PixelariaEngine.ECS;
using PixelariaEngine.Graphics;
using PixelariaEngine.Sandbox.Utils;

namespace PixelariaEngine.Sandbox.Drawable;

[Require(typeof(SpriteDrawer))]
public class FloatingIcon : Component
{
    public KeyboardIcon Icon { get; set; } = KeyboardIcon.E;
    public Vector2 Offset { get; set; }

    private SpriteSheet _spriteSheet;
    
    private SpriteDrawer _spriteDrawer;

    public override void OnAddedToEntity()
    {
        _spriteDrawer = Entity.GetComponent<SpriteDrawer>();
        _spriteSheet = Resources.LoadAsset<SpriteSheet>("SpriteSheets/keyboard");
        
        _spriteDrawer.SpriteSheet = _spriteSheet;
        _spriteDrawer.CurrentFrameIndex = (int)Icon;
        _spriteDrawer.PivotType = PivotType.Custom;
        
        var pivot = Vector2.Zero;

        if (_spriteSheet.TryGetFrame((int)Icon, out var frame))
        {
            pivot = PivotHelper.GetRelativePivot(PivotType.BottomCenter);
            
            pivot = new Vector2(pivot.X * frame.Width, pivot.Y * frame.Height);

            pivot += Offset;
        }

        _spriteDrawer.Pivot = pivot;
    }

    public void SetSpriteSheet(string path)
    {
        _spriteSheet = Resources.LoadAsset<SpriteSheet>(path);
        _spriteDrawer.SpriteSheet = _spriteSheet;
    }
    
    
}