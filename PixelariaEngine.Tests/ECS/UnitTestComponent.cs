using PixelariaEngine.ECS;

namespace PixelariaEngine.Tests;

public class UnitTestComponent : Component
{
    public int FrameCount = 0;
    public bool OnCreatedCalled;
    public bool OnDestroyedCalled;
    public bool OnAddedToEntityCalled;
    public bool OnRemovedFromEntityCalled;
    public bool OnEnabledCalled;
    public bool OnDisabledCalled;

    public override void OnUpdate()
    {
        FrameCount++;
    }

    public override void OnCreated()
    {
        OnCreatedCalled = true;
    }

    public override void OnDestroyed()
    {
        OnDestroyedCalled = true;
    }

    public override void OnAddedToEntity()
    {
        OnAddedToEntityCalled = true;
    }

    public override void OnRemovedFromEntity()
    {
        OnRemovedFromEntityCalled = true;
    }

    public override void OnEnabled()
    {
        OnEnabledCalled = true;
    }

    public override void OnDisabled()
    {
        OnDisabledCalled = true;
    }
}