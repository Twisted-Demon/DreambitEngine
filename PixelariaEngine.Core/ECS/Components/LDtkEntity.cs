using System;

namespace PixelariaEngine.ECS;

public class LDtkEntityComponent : Component<LDtkEntityComponent>
{
    public Guid Iid { get; internal set; }
}