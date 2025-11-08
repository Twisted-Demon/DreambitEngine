using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Vector2 = System.Numerics.Vector2;

namespace Dreambit.Examples.Pong;

[Require(typeof(RectDrawer), typeof(BoxCollider))]
public class PongPaddle : Component<PongPaddle>
{
    public PlayerNumber PlayerNumber { get; set; } = PlayerNumber.One;

    private RectDrawer _drawer;
    private BoxCollider _collider;

    public override void OnCreated()
    {
        _drawer = Entity.GetComponent<RectDrawer>();
        _drawer.Width = 24;
        _drawer.Height = 128;
        _drawer.Color = Color.White;
        _drawer.PivotType = PivotType.Center;
        
        _collider = Entity.GetComponent<BoxCollider>();
        _collider.SetShape(Box2D.CreateRectangle(Vector2.Zero, 12f, 64f));
    }
}

public enum PlayerNumber
{
    One,
    Two
}