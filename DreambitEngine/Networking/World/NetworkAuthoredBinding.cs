using System;

namespace Dreambit.Networking.World;

/// <summary>
/// Associates an authored entity's serialized source GUID with its runtime network identity
/// and owner for initial scene synchronization.
/// </summary>
/// <param name="SourceGuid">The stable entity GUID stored in the source scene or Blueprint.</param>
/// <param name="NetworkEntityId">The entity's runtime identity in the network scene.</param>
/// <param name="Owner">
/// The owning peer, or <see cref="NetworkPeerId.None"/> when the server owns the entity.
/// </param>
public readonly record struct NetworkAuthoredBinding(
    Guid SourceGuid,
    NetworkEntityId NetworkEntityId,
    NetworkPeerId Owner);
