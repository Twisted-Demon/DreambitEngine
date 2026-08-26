using System;

namespace Dreambit.Networking.Transport;

public readonly record struct TransportConnectionId(ulong Value)
{
    public static TransportConnectionId None => default;
    public bool IsValid => Value != 0;
    public override string ToString() => Value.ToString();
}

public enum NetworkDelivery : byte
{
    ReliableOrdered = 0,
    UnreliableSequenced = 1
}

public enum TransportState : byte
{
    Stopped = 0,
    Starting = 1,
    Listening = 2,
    Connecting = 3,
    Connected = 4,
    Stopping = 5,
    Faulted = 6,
    Disposed = 7
}

public enum TransportEventKind : byte
{
    Connected = 0,
    Data = 1,
    Disconnected = 2,
    Error = 3
}

public enum TransportDisconnectReason : ushort
{
    None = 0,
    LocalShutdown = 1,
    RemoteClosed = 2,
    ConnectionFailed = 3,
    TimedOut = 4,
    ProtocolError = 5,
    Incompatible = 6,
    Kicked = 7,
    TransportError = 8
}

public readonly record struct TransportCapabilities(
    int MaxReliablePayload,
    int MaxUnreliablePayload,
    byte MaxChannels)
{
    public TransportCapabilities Validate()
    {
        if (MaxReliablePayload <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxReliablePayload));
        if (MaxUnreliablePayload <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxUnreliablePayload));
        if (MaxChannels == 0)
            throw new ArgumentOutOfRangeException(nameof(MaxChannels));
        return this;
    }
}

/// <summary>
/// A transport event whose payload remains valid until the next call to
/// <see cref="INetworkTransport.TryPollEvent"/>. Consumers must decode or copy it immediately.
/// </summary>
public readonly record struct TransportEvent(
    TransportEventKind Kind,
    TransportConnectionId Connection,
    ReadOnlyMemory<byte> Payload,
    NetworkDelivery Delivery = NetworkDelivery.ReliableOrdered,
    byte Channel = 0,
    TransportDisconnectReason Reason = TransportDisconnectReason.None,
    string? Diagnostic = null);
