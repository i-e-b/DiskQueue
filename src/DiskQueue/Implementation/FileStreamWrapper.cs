using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

[assembly:InternalsVisibleTo("DiskQueue.Tests")]

namespace DiskQueue.Implementation
{
    internal class FileStreamWrapper : IFileStream, IBinaryReader
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
        
        public int ReadInt32()
        {
            // TODO: switch to manual. BinaryReader should always be little-endian.
            using var br = new BinaryReader(_base, Encoding.UTF8, leaveOpen:true);
            return br.ReadInt32();
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
            // TODO: switch to manual. BinaryReader should always be little-endian.
            using var br = new BinaryReader(_base, Encoding.UTF8, leaveOpen:true);
            return br.ReadInt64();
        }
    }
}