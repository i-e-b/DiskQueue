using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly:InternalsVisibleTo("DiskQueue.Tests")]

namespace DiskQueue.Implementation
{
    internal class FileStreamWrapper : IFileStream, IBinaryReader, IBinaryWriter
    {
        private readonly Stream _base;

        public FileStreamWrapper(Stream stream)
        {
            _base = stream;
        }

        public void Dispose() => _base.Dispose();

        public long Write(byte[] bytes)
        {
            _base.Write(bytes, 0, bytes.Length);
            return _base.Position;
        }

        public async Task<long> WriteAsync(byte[] bytes)
        {
            await _base.WriteAsync(bytes, 0, bytes.Length)!.ConfigureAwait(false)!;
            return _base.Position;
        }

        public void Flush()
        {
            if (_base is FileStream fs) fs.Flush(flushToDisk: true);
            else _base.Flush();
        }

        public void MoveTo(long offset) => _base.Seek(offset, SeekOrigin.Begin);
        public int Read(byte[] buffer, int offset, int length) => _base.Read(buffer, offset, length);
        public IBinaryReader GetBinaryReader() => this;
        public void SetLength(long length) => _base.SetLength(length);
        public void SetPosition(long position) => _base.Seek(position, SeekOrigin.Begin);

        public void Truncate()
        {
            SetLength(0);
        }

        public int ReadInt32()
        {
            var d = _base.ReadByte();
            var c = _base.ReadByte();
            var b = _base.ReadByte();
            var a = _base.ReadByte();
            if (a < 0 || b < 0 || c < 0 || d < 0) throw new EndOfStreamException(); // truncated
            
            return a << 24 | b << 16 | c << 8 | d;
        }

        public byte ReadByte()
        {
            return (byte)_base.ReadByte();
        }

        public byte[] ReadBytes(int count)
        {
            var buffer = new byte[count];
            var actual = _base.Read(buffer, 0, count);
            if (actual != count) return Array.Empty<byte>();
            return buffer;
        }

        public long GetLength()
        {
            return _base.Length;
        }

        public long GetPosition() => _base.Position;
        public long ReadInt64()
        {
            var b = (long)ReadInt32();
            var a = (long)ReadInt32();
            return a << 32 | b;
        }
    }
}