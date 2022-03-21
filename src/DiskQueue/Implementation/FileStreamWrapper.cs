using System;
using System.IO;

namespace DiskQueue.Implementation
{
    internal class FileStreamWrapper : IFileStream
    {
        private readonly FileStream _base;

        public FileStreamWrapper(FileStream stream)
        {
            _base = stream;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public long Write(byte[] bytes)
        {
            throw new NotImplementedException();
        }

        public void Flush()
        {
            throw new NotImplementedException();
        }

        public void MoveTo(long offset)
        {
            throw new NotImplementedException();
        }

        public int Read(byte[] buffer, int offset, int length)
        {
            throw new NotImplementedException();
        }

        public IBinaryReader GetBinaryReader()
        {
            throw new NotImplementedException();
        }

        public void SetLength(long length)
        {
            throw new NotImplementedException();
        }

        public void SetPosition(long position)
        {
            throw new NotImplementedException();
        }

        public int GetPosition()
        {
            throw new NotImplementedException();
        }
    }
}