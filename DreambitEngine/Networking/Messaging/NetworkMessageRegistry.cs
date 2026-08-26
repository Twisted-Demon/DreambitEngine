using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dreambit.Networking.Protocol;

namespace Dreambit.Networking.Messaging;

public sealed class NetworkMessageRegistry
{
    private readonly Dictionary<ushort, INetworkMessageRegistration> _byId = [];
    private readonly Dictionary<Type, INetworkMessageRegistration> _byType = [];
    private bool _frozen;

    public NetworkSchemaHash SchemaHash => BuildSchemaHash();

    public void Register<T>(
        ushort messageId,
        NetworkMessageDirection direction,
        int maximumPayload,
        INetworkMessageCodec<T> codec,
        Action<NetworkMessageContext, T> handler)
    {
        if (_frozen)
            throw new InvalidOperationException("Networking message registrations are frozen while a session is active.");
        if (messageId == 0)
            throw new ArgumentOutOfRangeException(nameof(messageId));
        if (maximumPayload is < 1 or > NetworkOptions.DefaultMaxProtocolPayload)
            throw new ArgumentOutOfRangeException(nameof(maximumPayload));
        ArgumentNullException.ThrowIfNull(codec);
        ArgumentNullException.ThrowIfNull(handler);
        if (_byId.ContainsKey(messageId))
            throw new InvalidOperationException($"Networking message ID {messageId} is already registered.");
        if (_byType.ContainsKey(typeof(T)))
            throw new InvalidOperationException($"Networking message type '{typeof(T).FullName}' is already registered.");

        var registration = new NetworkMessageRegistration<T>(
            messageId,
            direction,
            maximumPayload,
            codec,
            handler);
        _byId.Add(messageId, registration);
        _byType.Add(typeof(T), registration);
    }

    internal void Freeze() => _frozen = true;
    internal void Unfreeze() => _frozen = false;

    internal INetworkMessageRegistration GetById(ushort id) =>
        _byId.TryGetValue(id, out var registration)
            ? registration
            : throw new NetworkProtocolException($"Networking message ID {id} is not registered.");

    internal INetworkMessageRegistration GetByType(Type type) =>
        _byType.TryGetValue(type, out var registration)
            ? registration
            : throw new InvalidOperationException($"Networking message type '{type.FullName}' is not registered.");

    private NetworkSchemaHash BuildSchemaHash()
    {
        var schema = new StringBuilder();
        foreach (var registration in _byId.Values.OrderBy(item => item.Id))
        {
            schema.Append(registration.Id).Append('|')
                .Append((byte)registration.Direction).Append('|')
                .Append(registration.MaximumPayload).Append('|')
                .Append(registration.MessageType.AssemblyQualifiedName).Append('\n');
        }
        return NetworkSchemaHash.Compute(schema.ToString());
    }
}

internal interface INetworkMessageRegistration
{
    ushort Id { get; }
    Type MessageType { get; }
    NetworkMessageDirection Direction { get; }
    int MaximumPayload { get; }
    void Write(NetworkWriter writer, object message);
    void ReadAndHandle(ref NetworkReader reader, NetworkMessageContext context);
}

internal sealed class NetworkMessageRegistration<T> : INetworkMessageRegistration
{
    private readonly INetworkMessageCodec<T> _codec;
    private readonly Action<NetworkMessageContext, T> _handler;

    public NetworkMessageRegistration(
        ushort id,
        NetworkMessageDirection direction,
        int maximumPayload,
        INetworkMessageCodec<T> codec,
        Action<NetworkMessageContext, T> handler)
    {
        Id = id;
        Direction = direction;
        MaximumPayload = maximumPayload;
        _codec = codec;
        _handler = handler;
    }

    public ushort Id { get; }
    public Type MessageType => typeof(T);
    public NetworkMessageDirection Direction { get; }
    public int MaximumPayload { get; }

    public void Write(NetworkWriter writer, object message)
    {
        if (message is not T typed)
            throw new ArgumentException($"Expected networking message type '{typeof(T).FullName}'.", nameof(message));
        _codec.Write(writer, typed);
    }

    public void ReadAndHandle(ref NetworkReader reader, NetworkMessageContext context)
    {
        var message = _codec.Read(ref reader);
        reader.EnsureComplete();
        _handler(context, message);
    }
}
