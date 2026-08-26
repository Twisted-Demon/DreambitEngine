using Dreambit.Networking.Transport;

namespace Dreambit.Networking.Session;

internal enum NetworkConnectionPhase : byte
{
    AwaitingHello = 0,
    AwaitingWelcome = 1,
    Ready = 2,
    AwaitingSceneLoad = 3,
    Synchronizing = 4,
    Rejected = 5
}

internal sealed class NetworkPeer
{
    public required TransportConnectionId Connection { get; init; }
    public NetworkPeerId PeerId { get; set; }
    public NetworkConnectionPhase Phase { get; set; }
    public bool IsLocal { get; init; }
    public string? RemoteDiagnostic { get; set; }
}
