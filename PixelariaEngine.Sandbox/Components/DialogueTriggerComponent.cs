using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox;


public class DialogueTriggerComponent : Component
{
    private PolyShapeCollider _collider;
    

    public override void OnAddedToEntity()
    {
        _collider = Entity.GetComponent<PolyShapeCollider>();
        _collider.OnCollisionEnter += OnPlayerEnter;
        _collider.InterestedIn = ["player"];
    }

    public void OnPlayerEnter(Collider other)
    {
        if (!Entity.CompareTag(other, "player")) return;
        
        //DO STUFFF
        
    }
}