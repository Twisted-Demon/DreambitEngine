using Microsoft.Xna.Framework;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox.Components;

[Require(typeof(BoxCollider))]
public class FoliageDimmer : Component<FoliageDimmer>
{
    private SpriteDrawer _spriteDrawer;
    private BoxCollider _collider;

    public override void OnCreated()
    {
        _spriteDrawer = Entity.Parent?.GetComponent<SpriteDrawer>();
        _collider = Entity.GetComponent<BoxCollider>();
        _collider.isTrigger = true;

        if (_collider == null) return;
        
        _collider.OnCollisionEnter += OnCollisionEnter;
        _collider.OnCollisionExit += OnCollisionExit;
    }

    private void OnCollisionEnter(Collider other)
    {
        if (Entity.CompareTag(other, "player"))
            _spriteDrawer.Color = new Color(1, 1, 1, 0.25f);
    }

    private void OnCollisionExit(Collider other)
    {
        if (Entity.CompareTag(other, "player"))
            _spriteDrawer.Color = new Color(1, 1, 1, 1f);
    }
}