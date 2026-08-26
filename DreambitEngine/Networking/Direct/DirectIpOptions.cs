using System;
using System.Net;

namespace Dreambit.Networking.Direct;

public sealed class DirectIpOptions
{
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public int MaxReliablePayload { get; set; } = 1024 * 1024;
    public int MaxUnreliablePayload { get; set; } = 1150;
    public int MaxQueuedEvents { get; set; } = 2048;
    public byte MaxChannels { get; set; } = 4;

    internal void Validate()
    {
        if (ConnectionTimeout <= TimeSpan.Zero || ConnectionTimeout > TimeSpan.FromMinutes(5))
            throw new ArgumentOutOfRangeException(nameof(ConnectionTimeout));
        if (MaxReliablePayload is < 256 or > 16 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(MaxReliablePayload));
        if (MaxUnreliablePayload is < 64 or > 1400)
            throw new ArgumentOutOfRangeException(nameof(MaxUnreliablePayload));
        if (MaxQueuedEvents is < 16 or > 65_536)
            throw new ArgumentOutOfRangeException(nameof(MaxQueuedEvents));
        if (MaxChannels == 0)
            throw new ArgumentOutOfRangeException(nameof(MaxChannels));
    }
}
