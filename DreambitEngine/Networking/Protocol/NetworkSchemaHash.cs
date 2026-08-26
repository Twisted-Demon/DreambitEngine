using System;
using System.Security.Cryptography;
using System.Text;

namespace Dreambit.Networking.Protocol;

public readonly record struct NetworkSchemaHash(string Hex)
{
    public static NetworkSchemaHash Compute(string canonicalSchema)
    {
        ArgumentNullException.ThrowIfNull(canonicalSchema);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalSchema));
        return new NetworkSchemaHash(Convert.ToHexString(bytes));
    }

    public static NetworkSchemaHash Empty { get; } = Compute(string.Empty);

    public override string ToString() => Hex;
}
