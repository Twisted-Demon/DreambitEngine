using System;

namespace Dreambit.Networking;

public sealed class NetworkOptions
{
    public const int DefaultMaxProtocolPayload = 1024 * 1024;
    public const int DefaultMaxQueuedEvents = 1024;

    public string GameBuildId { get; set; } = "development";
    public string? ContentFingerprint { get; set; }
    public int MaxProtocolPayload { get; set; } = DefaultMaxProtocolPayload;
    public int MaxQueuedTransportEvents { get; set; } = DefaultMaxQueuedEvents;
    public int ReplicationRate { get; set; } = 20;
    public int MaxNetworkEntities { get; set; } = 100_000;
    public int MaxBaselineComponentRecords { get; set; } = 1_000_000;

    internal NetworkOptions Snapshot(string? defaultContentFingerprint = null) =>
        new()
        {
            GameBuildId = GameBuildId,
            ContentFingerprint = ContentFingerprint ?? defaultContentFingerprint,
            MaxProtocolPayload = MaxProtocolPayload,
            MaxQueuedTransportEvents = MaxQueuedTransportEvents,
            ReplicationRate = ReplicationRate,
            MaxNetworkEntities = MaxNetworkEntities,
            MaxBaselineComponentRecords = MaxBaselineComponentRecords
        };

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(GameBuildId))
            throw new InvalidOperationException("A non-empty networking GameBuildId is required.");
        if (GameBuildId.Length > 256)
            throw new InvalidOperationException("Networking GameBuildId must not exceed 256 characters.");
        if (ContentFingerprint is { Length: > 256 })
            throw new InvalidOperationException("Content fingerprint must not exceed 256 characters.");
        if (MaxProtocolPayload is < 256 or > 16 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(MaxProtocolPayload));
        if (MaxQueuedTransportEvents is < 1 or > 65_536)
            throw new ArgumentOutOfRangeException(nameof(MaxQueuedTransportEvents));
        if (ReplicationRate is < 1 or > 240)
            throw new ArgumentOutOfRangeException(nameof(ReplicationRate));
        if (MaxNetworkEntities is < 1 or > 1_000_000)
            throw new ArgumentOutOfRangeException(nameof(MaxNetworkEntities));
        if (MaxBaselineComponentRecords is < 1 or > 10_000_000)
            throw new ArgumentOutOfRangeException(nameof(MaxBaselineComponentRecords));
    }
}
