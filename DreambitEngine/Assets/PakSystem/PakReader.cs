using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

namespace Dreambit;

public sealed class PakReader
{
    public sealed class Entry
    {
        public required string Path;
        public required ushort Type;
        public required long Offset;
        public required long Size;
        public required uint Crc32;
    }

    private readonly FileStream _fs;
    private readonly Dictionary<string, Entry> _entries;

    public PakReader(string pakPath)
    {
        _fs = File.OpenRead(pakPath);
        _entries = ReadToc(_fs);
    }

    public bool TryOpen(string logicalPath, out Stream stream, out Entry entry)
    {
        var key = Normalize(logicalPath);
        if (_entries.TryGetValue(key, out var e))
        {
            stream = new SubStream(_fs, e.Offset, e.Size);
            entry = e;
            return true;
        }
        stream = null; entry = null; return false;
    }

    public Stream Open(string logicalPath)
    {
        if(!TryOpen(logicalPath, out var s, out _))
            throw new FileNotFoundException(logicalPath);

        return s!;
    }

    public void Dispose() => _fs.Dispose();
    
    private static string Normalize(string p) => p.Replace('\\','/').Trim().TrimStart('.', '/').ToLowerInvariant();

    private static Dictionary<string, Entry> ReadToc(FileStream fs)
    {
        Span<byte> b = stackalloc byte[16];
        fs.ReadExactly(b[..4]);
        if (b[0] != (byte)'P' || b[1] != (byte)'A' || b[2] != (byte)'K' || b[3] != (byte)'0')
            throw new InvalidDataException("Not a PAK0");

        fs.ReadExactly(b[..2]); var version = BinaryPrimitives.ReadUInt16LittleEndian(b[..2]);
        if (version != 1) throw new NotSupportedException($"PAK version {version}");

        fs.ReadExactly(b[..2]); var count = BinaryPrimitives.ReadUInt16LittleEndian(b[..2]);

        fs.ReadExactly(b[..8]); var tocOffset = (long)BinaryPrimitives.ReadUInt64LittleEndian(b[..8]);
        fs.ReadExactly(b[..8]); var dataOffset = (long)BinaryPrimitives.ReadUInt64LittleEndian(b[..8]);

        fs.Position = tocOffset;

        var map = new Dictionary<string, Entry>(count, StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < count; i++)
        {
            fs.ReadExactly(b[..2]); int pathLen = BinaryPrimitives.ReadUInt16LittleEndian(b[..2]);
            byte[] pbuf = GC.AllocateUninitializedArray<byte>(pathLen);
            fs.ReadExactly(pbuf);
            var path = System.Text.Encoding.UTF8.GetString(pbuf);

            fs.ReadExactly(b[..2]); var type = BinaryPrimitives.ReadUInt16LittleEndian(b[..2]);
            fs.ReadExactly(b[..8]); var off  = (long)BinaryPrimitives.ReadUInt64LittleEndian(b[..8]);
            fs.ReadExactly(b[..8]); var size = (long)BinaryPrimitives.ReadUInt64LittleEndian(b[..8]);
            fs.ReadExactly(b[..4]); var crc  = BinaryPrimitives.ReadUInt32LittleEndian(b[..4]);

            map[Normalize(path)] = new Entry { Path = path, Type = type, Offset = off, Size = size, Crc32 = crc };
        }
        return map;
    }

    
    // Non-owning view into a slice of a base stream
    private sealed class SubStream : Stream
    {
        private readonly Stream _base;
        private readonly long _start, _len;
        private long _pos;

        public SubStream(Stream @base, long start, long length)
        {
            _base = @base; _start = start; _len = length; _pos = 0;
        }
        public override long Length => _len;
        public override long Position { get => _pos; set => Seek(value, SeekOrigin.Begin); }
        public override bool CanRead => true; public override bool CanSeek => true; public override bool CanWrite => false;
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_pos >= _len) return 0;
            count = (int)Math.Min(count, _len - _pos);
            lock (_base) { _base.Position = _start + _pos; var n = _base.Read(buffer, offset, count); _pos += n; return n; }
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            long target = origin switch
            {
                SeekOrigin.Begin   => offset,
                SeekOrigin.Current => _pos + offset,
                SeekOrigin.End     => _len + offset,
                _ => _pos
            };
            _pos = Math.Clamp(target, 0, _len);
            return _pos;
        }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}