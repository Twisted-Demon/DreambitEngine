using Dreambit.ECS;

namespace Dreambit.Sandbox;


public class DialogueTriggerComponent : Component
{
    private PolyShapeCollider _collider;
    public string CutscenePath = string.Empty;
    

    public override void OnAddedToEntity()
    {
        _collider = Entity.GetComponent<PolyShapeCollider>();
        _collider.IsTrigger = true;
        _collider.OnCollisionEnter += OnPlayerEnter;
        _collider.InterestedIn = ["player"];
    }

    public void OnPlayerEnter(Collider other)
    {
        if (!Entity.CompareTag(other, "player")) return;
        Scene.StartCutscene(CutscenePath, "");
        Scene.DestroyEntity(Entity);
    }
}