using System;

namespace Dreambit.Networking.World;

public readonly record struct NetworkAuthoredBinding(
    Guid SourceGuid,
    NetworkEntityId NetworkEntityId,
    NetworkPeerId Owner);
