using System;
using Dreambit.ECS;

namespace Dreambit.LDtk.ECS;

public class LDtkIid : Component
{
    public Guid Iid { get; internal set; }

    public override void OnCreated()
    {
        LDtkManager.Instance.RegisterEntity(Iid, Entity);
    }

    public override void OnDestroyed()
    {
        LDtkManager.Instance.DeregisterEntity(Iid);
    }
}
