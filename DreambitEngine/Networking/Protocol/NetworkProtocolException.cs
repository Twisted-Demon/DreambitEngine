using System;

namespace Dreambit.Networking.Protocol;

public sealed class NetworkProtocolException : Exception
{
    public NetworkProtocolException(string message)
        : base(message)
    {
    }

    public NetworkProtocolException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
