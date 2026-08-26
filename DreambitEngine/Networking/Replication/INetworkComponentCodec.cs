using Dreambit.ECS;
using Dreambit.Networking.Protocol;

namespace Dreambit.Networking.Replication;

/// <summary>Explicit serializer for a complex replicated Component.</summary>
public interface INetworkComponentCodec<T> where T : Component
{
    void Write(NetworkWriter writer, T component);
    void Read(ref NetworkReader reader, T component);
}
