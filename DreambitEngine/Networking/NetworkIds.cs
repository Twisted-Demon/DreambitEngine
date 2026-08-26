using System;

namespace Dreambit.Networking;

public readonly record struct NetworkPeerId(uint Value)
{
    public static NetworkPeerId None => default;
    public bool IsValid => Value != 0;
    public override string ToString() => Value.ToString();
}

public readonly record struct NetworkEntityId(ulong Value)
{
    public static NetworkEntityId None => default;
    public bool IsValid => Value != 0;
    public override string ToString() => Value.ToString();
}

public readonly record struct NetworkSceneEpoch(uint Value)
{
    public static NetworkSceneEpoch None => default;
    public bool IsValid => Value != 0;
    public override string ToString() => Value.ToString();
}

public readonly record struct NetworkStructuralRevision(ulong Value)
{
    public static NetworkStructuralRevision None => default;
    public override string ToString() => Value.ToString();
}

public readonly record struct NetworkEntityRef(
    NetworkSceneEpoch SceneEpoch,
    NetworkEntityId EntityId)
{
    public static NetworkEntityRef None => default;
    public bool IsValid => SceneEpoch.IsValid && EntityId.IsValid;
}
