using System;

namespace Dreambit.ECS;

public class LDtkIid : Component
{
    public Guid Iid { get; internal set; }

    public override void OnCreated()
    {
        LDtkManager.RegisterEntity(Iid, Entity);
    }

    public override void OnDestroyed()
    {
        LDtkManager.DeregisterEntity(Iid);
    }
}