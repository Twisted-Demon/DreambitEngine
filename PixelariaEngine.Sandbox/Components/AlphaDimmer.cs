using Microsoft.Xna.Framework;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox.Components;

[Require(typeof(BoxCollider))]
public class AlphaDimmer : Component<AlphaDimmer>
{
    private SpriteDrawer _spriteDrawer;
    private BoxCollider _collider;

    public override void OnCreated()
    {
        _spriteDrawer = Entity.Parent?.GetComponent<SpriteDrawer>();
        _collider = Entity.GetComponent<BoxCollider>();
        _collider.IsTrigger = true;

        if (_collider == null) return;
        
        _collider.OnCollisionEnter += OnCollisionEnter;
        _collider.OnCollisionExit += OnCollisionExit;
    }

    private bool _isDimming = false;
    private float _alpha = 1.0f;
    public float DimSpeed = 3.5f;
    
    public override void OnUpdate()
    {
        float dimAmount = Time.DeltaTime * DimSpeed;
        if (_isDimming)
        {
            _alpha = Mathf.Clamp(_alpha -= dimAmount, .2f, 1f);
            _spriteDrawer.Color = new Color(1, 1, 1, _alpha);
        }
        else
        {
            _alpha = Mathf.Clamp(_alpha += dimAmount, .2f, 1f);
            _spriteDrawer.Color = new Color(1, 1, 1, _alpha);
        }
    }

    private void OnCollisionEnter(Collider other)
    {
        if (Entity.CompareTag(other, "player"))
            _isDimming = true;
    }

    private void OnCollisionExit(Collider other)
    {
        if (Entity.CompareTag(other, "player"))
            _isDimming = false;
    }
}