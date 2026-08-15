using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ZstdSharp;

namespace Dreambit;

public sealed class PakReader : IDisposable
{
    private const ushort LegacyVersion = 1;
    private const ushort CurrentVersion = 2;

    private readonly Dictionary<string, Entry> _entries;
    private readonly FileStream _fs;

    public PakReader(string pakPath)
    {
        _fs = new FileStream(
            pakPath,
            FileMode.Open,
            FileAccess.Read,
            // Windows' overwrite implementation opens the destination for
            // write access before atomically replacing it. Keep the old file
            // readable while allowing the Asset Baker to publish a new PAK.
            FileShare.ReadWrite | FileShare.Delete);

        _entries = ReadToc(_fs);
    }

    public bool TryOpen(
        string logicalPath,
        out Stream stream,
        out Entry entry)
    {
        var key = Normalize(logicalPath);

        if (!_entries.TryGetValue(key, out var e))
        {
            stream = null;
            entry = null;
            return false;
        }

        var storedStream = new SubStream(
            _fs,
            e.Offset,
            e.StoredSize);

        switch (e.Compression)
        {
            case PakCompression.None:
                stream = storedStream;
                break;

            case PakCompression.Zstd:
                // Decompression happens incrementally as the asset loader
                // reads from this stream. We do not inflate the entire
                // asset into another MemoryStream first.
                stream = new DecompressionStream(
                    storedStream,
                    leaveOpen: false);
                break;

            default:
                storedStream.Dispose();

                throw new InvalidDataException(
                    $"PAK entry '{e.Path}' uses unsupported " +
                    $"compression method {(ushort)e.Compression}.");
        }

        entry = e;
        return true;
    }

    public Stream Open(string logicalPath)
    {
        if (!TryOpen(logicalPath, out var stream, out _))
            throw new FileNotFoundException(logicalPath);

        return stream;
    }

    public void Dispose()
    {
        _fs.Dispose();
    }

    private static string Normalize(string path)
    {
        return path
            .Replace('\\', '/')
            .Trim()
            .TrimStart('.', '/')
            .ToLowerInvariant();
    }

    private static Dictionary<string, Entry> ReadToc(
        FileStream fs)
    {
        Span<byte> buffer = stackalloc byte[16];

        fs.ReadExactly(buffer[..4]);

        if (buffer[0] != (byte)'P' ||
            buffer[1] != (byte)'A' ||
            buffer[2] != (byte)'K' ||
            buffer[3] != (byte)'0')
        {
            throw new InvalidDataException("Not a PAK0");
        }

        fs.ReadExactly(buffer[..2]);

        var version =
            BinaryPrimitives.ReadUInt16LittleEndian(
                buffer[..2]);

        if (version is not LegacyVersion and not CurrentVersion)
        {
            throw new NotSupportedException(
                $"PAK version {version}");
        }

        fs.ReadExactly(buffer[..2]);

        var count =
            BinaryPrimitives.ReadUInt16LittleEndian(
                buffer[..2]);

        fs.ReadExactly(buffer[..8]);

        var tocOffset =
            CheckedInt64(
                BinaryPrimitives.ReadUInt64LittleEndian(
                    buffer[..8]),
                "TOC offset");

        fs.ReadExactly(buffer[..8]);

        var dataOffset =
            CheckedInt64(
                BinaryPrimitives.ReadUInt64LittleEndian(
                    buffer[..8]),
                "data offset");

        if (tocOffset < 24 ||
            dataOffset < tocOffset ||
            dataOffset > fs.Length)
        {
            throw new InvalidDataException(
                "PAK contains invalid TOC/data offsets.");
        }

        fs.Position = tocOffset;

        var map = new Dictionary<string, Entry>(
            count,
            StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < count; i++)
        {
            fs.ReadExactly(buffer[..2]);

            var pathLength =
                BinaryPrimitives.ReadUInt16LittleEndian(
                    buffer[..2]);

            var pathBytes =
                GC.AllocateUninitializedArray<byte>(
                    pathLength);

            fs.ReadExactly(pathBytes);

            var path =
                Encoding.UTF8.GetString(pathBytes);

            fs.ReadExactly(buffer[..2]);

            var type =
                BinaryPrimitives.ReadUInt16LittleEndian(
                    buffer[..2]);

            PakCompression compression;
            long offset;
            long storedSize;
            long uncompressedSize;
            uint crc;

            if (version == LegacyVersion)
            {
                /*
                 * PAK v1:
                 *
                 * ushort type
                 * ulong  offset
                 * ulong  size
                 * uint   crc32
                 */

                compression = PakCompression.None;

                fs.ReadExactly(buffer[..8]);

                offset = CheckedInt64(
                    BinaryPrimitives.ReadUInt64LittleEndian(
                        buffer[..8]),
                    "entry offset");

                fs.ReadExactly(buffer[..8]);

                storedSize = CheckedInt64(
                    BinaryPrimitives.ReadUInt64LittleEndian(
                        buffer[..8]),
                    "entry size");

                uncompressedSize = storedSize;

                fs.ReadExactly(buffer[..4]);

                crc =
                    BinaryPrimitives.ReadUInt32LittleEndian(
                        buffer[..4]);
            }
            else
            {
                /*
                 * PAK v2:
                 *
                 * ushort type
                 * ushort compression
                 * ulong  offset
                 * ulong  storedSize
                 * ulong  uncompressedSize
                 * uint   crc32
                 */

                fs.ReadExactly(buffer[..2]);

                var compressionValue =
                    BinaryPrimitives.ReadUInt16LittleEndian(
                        buffer[..2]);

                if (compressionValue >
                    (ushort)PakCompression.Zstd)
                {
                    throw new InvalidDataException(
                        $"PAK entry '{path}' contains unknown " +
                        $"compression method {compressionValue}.");
                }

                compression =
                    (PakCompression)compressionValue;

                fs.ReadExactly(buffer[..8]);

                offset = CheckedInt64(
                    BinaryPrimitives.ReadUInt64LittleEndian(
                        buffer[..8]),
                    "entry offset");

                fs.ReadExactly(buffer[..8]);

                storedSize = CheckedInt64(
                    BinaryPrimitives.ReadUInt64LittleEndian(
                        buffer[..8]),
                    "stored size");

                fs.ReadExactly(buffer[..8]);

                uncompressedSize = CheckedInt64(
                    BinaryPrimitives.ReadUInt64LittleEndian(
                        buffer[..8]),
                    "uncompressed size");

                fs.ReadExactly(buffer[..4]);

                crc =
                    BinaryPrimitives.ReadUInt32LittleEndian(
                        buffer[..4]);
            }

            if (offset < dataOffset ||
                storedSize < 0 ||
                offset > fs.Length ||
                storedSize > fs.Length - offset)
            {
                throw new InvalidDataException(
                    $"PAK entry '{path}' points outside the PAK.");
            }

            if (compression == PakCompression.None &&
                storedSize != uncompressedSize)
            {
                throw new InvalidDataException(
                    $"Uncompressed PAK entry '{path}' has " +
                    "different stored and uncompressed sizes.");
            }

            var normalizedPath = Normalize(path);

            var pakEntry = new Entry
            {
                Path = path,
                Type = type,
                Compression = compression,
                Offset = offset,

                // Preserve Size as the logical size of the asset presented
                // to callers.
                Size = uncompressedSize,

                StoredSize = storedSize,
                Crc32 = crc
            };

            if (!map.TryAdd(
                    normalizedPath,
                    pakEntry))
            {
                throw new InvalidDataException(
                    $"PAK contains duplicate case-insensitive path '{path}'.");
            }
        }

        return map;
    }

    private static long CheckedInt64(
        ulong value,
        string fieldName)
    {
        if (value > long.MaxValue)
        {
            throw new InvalidDataException(
                $"PAK {fieldName} exceeds Int64.MaxValue.");
        }

        return (long)value;
    }

    public sealed class Entry
    {
        public required uint Crc32;
        public required long Offset;
        public required string Path;

        /// <summary>
        /// Logical uncompressed size of the asset.
        /// </summary>
        public required long Size;

        /// <summary>
        /// Number of physical bytes occupied in the PAK.
        /// </summary>
        public required long StoredSize;

        public required ushort Type;

        public required PakCompression Compression;
    }

    // Non-owning view into a slice of the base PAK stream.
    private sealed class SubStream : Stream
    {
        private readonly Stream _base;
        private readonly long _start;

        private long _position;

        public SubStream(
            Stream @base,
            long start,
            long length)
        {
            _base = @base;
            _start = start;

            Length = length;
            _position = 0;
        }

        public override long Length { get; }

        public override long Position
        {
            get => _position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override void Flush()
        {
        }

        public override int Read(
            byte[] buffer,
            int offset,
            int count)
        {
            return Read(
                buffer.AsSpan(
                    offset,
                    count));
        }

        public override int Read(
            Span<byte> buffer)
        {
            if (_position >= Length)
                return 0;

            var count = (int)Math.Min(
                buffer.Length,
                Length - _position);

            lock (_base)
            {
                _base.Position =
                    _start + _position;

                var bytesRead =
                    _base.Read(buffer[..count]);

                _position += bytesRead;

                return bytesRead;
            }
        }

        public override long Seek(
            long offset,
            SeekOrigin origin)
        {
            var target = origin switch
            {
                SeekOrigin.Begin =>
                    offset,

                SeekOrigin.Current =>
                    _position + offset,

                SeekOrigin.End =>
                    Length + offset,

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(origin))
            };

            _position =
                Math.Clamp(
                    target,
                    0,
                    Length);

            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(
            byte[] buffer,
            int offset,
            int count)
        {
            throw new NotSupportedException();
        }
    }
}
