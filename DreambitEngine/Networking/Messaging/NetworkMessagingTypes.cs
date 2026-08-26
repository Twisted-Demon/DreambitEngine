using Dreambit.Networking.Protocol;

namespace Dreambit.Networking.Messaging;

public enum NetworkMessageDirection : byte
{
    ClientToServer = 0,
    ServerToClient = 1,
    Bidirectional = 2
}

public readonly record struct NetworkMessageContext(
    NetworkPeerId Sender,
    NetworkSceneEpoch SceneEpoch,
    ulong ServerTick);

public interface INetworkMessageCodec<T>
{
    void Write(NetworkWriter writer, T message);
    T Read(ref NetworkReader reader);
}
