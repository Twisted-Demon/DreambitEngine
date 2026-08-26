using Microsoft.Xna.Framework;

namespace Dreambit.Networking;

/// <summary>Server-authored runtime overrides included with a network Blueprint spawn.</summary>
public sealed class NetworkSpawnOptions
{
    public NetworkPeerId Owner { get; set; }
    public bool DestroyWithOwner { get; set; } = true;
    public bool? Enabled { get; set; }
    public Vector3? Position { get; set; }
    public Vector3? Rotation { get; set; }
    public Vector3? Scale { get; set; }
}
