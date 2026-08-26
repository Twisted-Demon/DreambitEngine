using System;

namespace Dreambit.Networking.Transport;

/// <summary>
/// Moves bounded messages between opaque transport connections. Implementations must never
/// invoke Dreambit scene or ECS APIs from transport callbacks or worker threads.
/// </summary>
public interface INetworkTransport : IDisposable
{
    TransportCapabilities Capabilities { get; }
    TransportState State { get; }

    void StartServer();
    void Connect();
    bool TryPollEvent(out TransportEvent transportEvent);
    void Send(
        TransportConnectionId connection,
        ReadOnlySpan<byte> payload,
        NetworkDelivery delivery,
        byte channel);
    void Disconnect(
        TransportConnectionId connection,
        TransportDisconnectReason reason = TransportDisconnectReason.LocalShutdown);
    void Stop();
}
