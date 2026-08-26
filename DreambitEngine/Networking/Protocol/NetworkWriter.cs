using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Dreambit.Networking.Protocol;

public sealed class NetworkWriter : IDisposable
{
    private byte[]? _buffer;
    private readonly int _maximumLength;
    private int _length;

    public NetworkWriter(int initialCapacity = 256, int maximumLength = NetworkOptions.DefaultMaxProtocolPayload)
    {
        if (initialCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        if (maximumLength < initialCapacity)
            throw new ArgumentOutOfRangeException(nameof(maximumLength));

        _maximumLength = maximumLength;
        _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
    }

    public int Length => _length;
    public ReadOnlySpan<byte> WrittenSpan => Buffer.AsSpan(0, _length);
    public ReadOnlyMemory<byte> WrittenMemory => Buffer.AsMemory(0, _length);

    private byte[] Buffer => _buffer ?? throw new ObjectDisposedException(nameof(NetworkWriter));

    public void WriteByte(byte value)
    {
        Ensure(1);
        Buffer[_length++] = value;
    }

    public void WriteSByte(sbyte value) => WriteByte(unchecked((byte)value));

    public void WriteInt16(short value)
    {
        Ensure(sizeof(short));
        BinaryPrimitives.WriteInt16LittleEndian(Buffer.AsSpan(_length), value);
        _length += sizeof(short);
    }

    public void WriteBoolean(bool value) => WriteByte(value ? (byte)1 : (byte)0);

    public void WriteUInt16(ushort value)
    {
        Ensure(sizeof(ushort));
        BinaryPrimitives.WriteUInt16LittleEndian(Buffer.AsSpan(_length), value);
        _length += sizeof(ushort);
    }

    public void WriteInt32(int value)
    {
        Ensure(sizeof(int));
        BinaryPrimitives.WriteInt32LittleEndian(Buffer.AsSpan(_length), value);
        _length += sizeof(int);
    }

    public void WriteUInt32(uint value)
    {
        Ensure(sizeof(uint));
        BinaryPrimitives.WriteUInt32LittleEndian(Buffer.AsSpan(_length), value);
        _length += sizeof(uint);
    }

    public void WriteInt64(long value)
    {
        Ensure(sizeof(long));
        BinaryPrimitives.WriteInt64LittleEndian(Buffer.AsSpan(_length), value);
        _length += sizeof(long);
    }

    public void WriteUInt64(ulong value)
    {
        Ensure(sizeof(ulong));
        BinaryPrimitives.WriteUInt64LittleEndian(Buffer.AsSpan(_length), value);
        _length += sizeof(ulong);
    }

    public void WriteSingle(float value) => WriteInt32(BitConverter.SingleToInt32Bits(value));
    public void WriteDouble(double value) => WriteInt64(BitConverter.DoubleToInt64Bits(value));

    public void WriteGuid(Guid value)
    {
        Ensure(16);
        if (!value.TryWriteBytes(Buffer.AsSpan(_length, 16)))
            throw new InvalidOperationException("Could not encode a GUID.");
        _length += 16;
    }

    public void WriteBytes(ReadOnlySpan<byte> value)
    {
        Ensure(value.Length);
        value.CopyTo(Buffer.AsSpan(_length));
        _length += value.Length;
    }

    public void WriteLengthPrefixedBytes(ReadOnlySpan<byte> value, int maximumLength)
    {
        if (value.Length > maximumLength)
            throw new NetworkProtocolException($"Byte payload length {value.Length} exceeds {maximumLength}.");
        WriteInt32(value.Length);
        WriteBytes(value);
    }

    public void WriteString(string? value, int maximumUtf8Bytes = 4096)
    {
        if (value is null)
        {
            WriteInt32(-1);
            return;
        }

        var byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount > maximumUtf8Bytes)
            throw new NetworkProtocolException($"UTF-8 string length {byteCount} exceeds {maximumUtf8Bytes}.");

        WriteInt32(byteCount);
        Ensure(byteCount);
        Encoding.UTF8.GetBytes(value, Buffer.AsSpan(_length, byteCount));
        _length += byteCount;
    }

    public byte[] ToArray() => WrittenSpan.ToArray();

    public void Dispose()
    {
        var buffer = _buffer;
        _buffer = null;
        _length = 0;
        if (buffer is not null)
            ArrayPool<byte>.Shared.Return(buffer);
    }

    private void Ensure(int additionalLength)
    {
        if (additionalLength < 0 || _length > _maximumLength - additionalLength)
            throw new NetworkProtocolException(
                $"Network payload exceeds the configured maximum of {_maximumLength} bytes.");

        var buffer = Buffer;
        var required = _length + additionalLength;
        if (required <= buffer.Length)
            return;

        var newLength = Math.Min(
            _maximumLength,
            Math.Max(required, Math.Min(_maximumLength, buffer.Length * 2)));
        var replacement = ArrayPool<byte>.Shared.Rent(newLength);
        buffer.AsSpan(0, _length).CopyTo(replacement);
        _buffer = replacement;
        ArrayPool<byte>.Shared.Return(buffer);
    }
}
