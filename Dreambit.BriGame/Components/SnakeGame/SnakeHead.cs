using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit.BriGame.Components;

[Require(typeof(SpriteDrawer))]
public class SnakeHead : Component
{
    public SpriteDrawer Drawer;

    private SpriteSheet _upSheet;
    private SpriteSheet _downSheet;
    private SpriteSheet _leftSheet;
    private SpriteSheet _rightSheet;
    
    static Point Up    => new Point(0, -1);
    static Point Down  => new Point(0,  1);
    static Point Left  => new Point(-1, 0);
    static Point Right => new Point(1,  0);

    public override void OnAddedToEntity()
    {
        Drawer = Entity.GetComponent<SpriteDrawer>();
        
        _upSheet = SpriteSheet.Create(1, 1, "Textures/SnakeGame/head_up");
        _downSheet = SpriteSheet.Create(1, 1, "Textures/SnakeGame/head_down");
        _leftSheet = SpriteSheet.Create(1, 1, "Textures/SnakeGame/head_left");
        _rightSheet = SpriteSheet.Create(1, 1, "Textures/SnakeGame/head_right");
        
        Drawer.SpriteSheet = _upSheet;
    }

    public void SetDirection(Point direction)
    {
        if(direction == Right)
            Drawer.SpriteSheet = _rightSheet;
        if(direction == Left)
            Drawer.SpriteSheet = _leftSheet;
        if(direction == Up)
            Drawer.SpriteSheet = _upSheet;
        if(direction == Down)
            Drawer.SpriteSheet = _downSheet;
    }
    
}