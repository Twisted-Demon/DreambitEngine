using System;
using System.Buffers.Binary;
using System.Text;

namespace Dreambit.Networking.Protocol;

public ref struct NetworkReader
{
    private readonly ReadOnlySpan<byte> _buffer;
    private int _offset;

    public NetworkReader(ReadOnlySpan<byte> buffer)
    {
        _buffer = buffer;
        _offset = 0;
    }

    public int Remaining => _buffer.Length - _offset;
    public bool IsComplete => _offset == _buffer.Length;

    public byte ReadByte()
    {
        Require(1);
        return _buffer[_offset++];
    }

    public sbyte ReadSByte() => unchecked((sbyte)ReadByte());

    public short ReadInt16()
    {
        Require(sizeof(short));
        var value = BinaryPrimitives.ReadInt16LittleEndian(_buffer[_offset..]);
        _offset += sizeof(short);
        return value;
    }

    public bool ReadBoolean()
    {
        var value = ReadByte();
        return value switch
        {
            0 => false,
            1 => true,
            _ => throw new NetworkProtocolException($"Invalid Boolean value {value}.")
        };
    }

    public ushort ReadUInt16()
    {
        Require(sizeof(ushort));
        var value = BinaryPrimitives.ReadUInt16LittleEndian(_buffer[_offset..]);
        _offset += sizeof(ushort);
        return value;
    }

    public int ReadInt32()
    {
        Require(sizeof(int));
        var value = BinaryPrimitives.ReadInt32LittleEndian(_buffer[_offset..]);
        _offset += sizeof(int);
        return value;
    }

    public uint ReadUInt32()
    {
        Require(sizeof(uint));
        var value = BinaryPrimitives.ReadUInt32LittleEndian(_buffer[_offset..]);
        _offset += sizeof(uint);
        return value;
    }

    public long ReadInt64()
    {
        Require(sizeof(long));
        var value = BinaryPrimitives.ReadInt64LittleEndian(_buffer[_offset..]);
        _offset += sizeof(long);
        return value;
    }

    public ulong ReadUInt64()
    {
        Require(sizeof(ulong));
        var value = BinaryPrimitives.ReadUInt64LittleEndian(_buffer[_offset..]);
        _offset += sizeof(ulong);
        return value;
    }

    public float ReadSingle() => BitConverter.Int32BitsToSingle(ReadInt32());
    public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadInt64());

    public Guid ReadGuid()
    {
        Require(16);
        var value = new Guid(_buffer.Slice(_offset, 16));
        _offset += 16;
        return value;
    }

    public ReadOnlySpan<byte> ReadBytes(int length)
    {
        if (length < 0)
            throw new NetworkProtocolException($"Negative byte length {length}.");
        Require(length);
        var value = _buffer.Slice(_offset, length);
        _offset += length;
        return value;
    }

    public ReadOnlySpan<byte> ReadLengthPrefixedBytes(int maximumLength)
    {
        var length = ReadInt32();
        if (length < 0 || length > maximumLength)
            throw new NetworkProtocolException($"Byte payload length {length} is outside 0..{maximumLength}.");
        return ReadBytes(length);
    }

    public string? ReadString(int maximumUtf8Bytes = 4096)
    {
        var length = ReadInt32();
        if (length == -1)
            return null;
        if (length < 0 || length > maximumUtf8Bytes)
            throw new NetworkProtocolException($"UTF-8 string length {length} is outside 0..{maximumUtf8Bytes}.");
        return Encoding.UTF8.GetString(ReadBytes(length));
    }

    public void EnsureComplete()
    {
        if (!IsComplete)
            throw new NetworkProtocolException($"Network payload contains {Remaining} trailing bytes.");
    }

    private void Require(int count)
    {
        if (count < 0 || count > Remaining)
            throw new NetworkProtocolException(
                $"Network payload ended unexpectedly; requested {count} bytes with {Remaining} remaining.");
    }
}
