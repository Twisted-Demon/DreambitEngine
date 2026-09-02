using System;
using System.Text;

namespace Dreambit.Networking;

/// <summary>
/// Configures protocol compatibility, replication cadence, and defensive resource limits for
/// the next network session. <see cref="NetworkService"/> snapshots these values when a session
/// starts, so later edits apply to the next session.
/// </summary>
public sealed class NetworkOptions
{
    /// <summary>Default maximum encoded protocol payload: 1 MiB.</summary>
    public const int DefaultMaxProtocolPayload = 1024 * 1024;

    /// <summary>Default maximum number of transport events waiting for main-thread processing.</summary>
    public const int DefaultMaxQueuedEvents = 1024;

    /// <summary>
    /// Gets or sets the game-defined build identifier compared during the connection handshake.
    /// Peers with different values are rejected. The UTF-8 representation must be 256 bytes or less.
    /// </summary>
    public string GameBuildId { get; set; } = "development";

    /// <summary>
    /// Gets or sets the baked-content fingerprint compared during the connection handshake.
    /// When <see langword="null"/>, Dreambit uses <see cref="Resources.ContentFingerprint"/>
    /// when the session starts. The UTF-8 representation must be 256 bytes or less.
    /// </summary>
    public string? ContentFingerprint { get; set; }

    /// <summary>
    /// Gets or sets the maximum decoded protocol payload in bytes. Valid values are 256 bytes
    /// through 16 MiB. Individual transport limits may be smaller.
    /// </summary>
    public int MaxProtocolPayload { get; set; } = DefaultMaxProtocolPayload;

    /// <summary>
    /// Gets or sets the maximum number of copied transport events awaiting main-thread protocol
    /// processing. Valid values are 1 through 65,536.
    /// </summary>
    public int MaxQueuedTransportEvents { get; set; } = DefaultMaxQueuedEvents;

    /// <summary>
    /// Gets or sets the target authoritative snapshot frequency in updates per second.
    /// Valid values are 1 through 240.
    /// </summary>
    public int ReplicationRate { get; set; } = 20;

    /// <summary>
    /// Gets or sets the maximum number of registered network entities in one scene epoch.
    /// Valid values are 1 through 1,000,000.
    /// </summary>
    public int MaxNetworkEntities { get; set; } = 100_000;

    /// <summary>
    /// Gets or sets the maximum number of component-state records accepted in an initial world
    /// baseline. Valid values are 1 through 10,000,000.
    /// </summary>
    public int MaxBaselineComponentRecords { get; set; } = 1_000_000;

    /// <summary>Maximum registered replication scopes in one Scene epoch, including Global.</summary>
    public int MaxReplicationScopes { get; set; } = 1024;

    /// <summary>Maximum additive scope subscriptions tracked for one remote peer.</summary>
    public int MaxScopeSubscriptionsPerPeer { get; set; } = 256;

    /// <summary>Maximum UTF-8 bytes accepted for a scoped Scene Blueprint fallback name.</summary>
    public int MaxScopeAssetNameBytes { get; set; } = 1024;

    /// <summary>Maximum authored network entities accepted in one additive scope baseline.</summary>
    public int MaxScopedAuthoredEntities { get; set; } = 100_000;

    /// <summary>Maximum component-state records accepted in one additive scope baseline.</summary>
    public int MaxScopeBaselineComponentRecords { get; set; } = 1_000_000;

    internal NetworkOptions Snapshot(string? defaultContentFingerprint = null) =>
        new()
        {
            GameBuildId = GameBuildId,
            ContentFingerprint = ContentFingerprint ?? defaultContentFingerprint,
            MaxProtocolPayload = MaxProtocolPayload,
            MaxQueuedTransportEvents = MaxQueuedTransportEvents,
            ReplicationRate = ReplicationRate,
            MaxNetworkEntities = MaxNetworkEntities,
            MaxBaselineComponentRecords = MaxBaselineComponentRecords,
            MaxReplicationScopes = MaxReplicationScopes,
            MaxScopeSubscriptionsPerPeer = MaxScopeSubscriptionsPerPeer,
            MaxScopeAssetNameBytes = MaxScopeAssetNameBytes,
            MaxScopedAuthoredEntities = MaxScopedAuthoredEntities,
            MaxScopeBaselineComponentRecords = MaxScopeBaselineComponentRecords
        };

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(GameBuildId))
            throw new InvalidOperationException("A non-empty networking GameBuildId is required.");
        if (Encoding.UTF8.GetByteCount(GameBuildId) > 256)
            throw new InvalidOperationException("Networking GameBuildId must not exceed 256 UTF-8 bytes.");
        if (ContentFingerprint is not null && Encoding.UTF8.GetByteCount(ContentFingerprint) > 256)
            throw new InvalidOperationException("Content fingerprint must not exceed 256 UTF-8 bytes.");
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
        if (MaxReplicationScopes is < 1 or > 1_000_000)
            throw new ArgumentOutOfRangeException(nameof(MaxReplicationScopes));
        if (MaxScopeSubscriptionsPerPeer is < 1 or > 1_000_000)
            throw new ArgumentOutOfRangeException(nameof(MaxScopeSubscriptionsPerPeer));
        if (MaxScopeAssetNameBytes is < 1 or > 65_536)
            throw new ArgumentOutOfRangeException(nameof(MaxScopeAssetNameBytes));
        if (MaxScopedAuthoredEntities is < 1 or > 1_000_000)
            throw new ArgumentOutOfRangeException(nameof(MaxScopedAuthoredEntities));
        if (MaxScopeBaselineComponentRecords is < 1 or > 10_000_000)
            throw new ArgumentOutOfRangeException(nameof(MaxScopeBaselineComponentRecords));
    }
}
