using System;
using System.IO;

namespace DiskQueue.Implementation
{
    /// <summary>
    /// An inter-process lock based on a file.
    /// This is based on holding an open file stream
    /// </summary>
    public class LockFile : ILockFile, IDisposable
    {
        private readonly FileStream _stream;
        private readonly string _path;

        /// <summary>
        /// Create a new filesystem based lock
        /// </summary>
        public LockFile(FileStream stream, string path)
        {
            _stream = stream;
            _path = path;
        }

        /// <summary>
        /// Remove a filesystem based lock (releases the lock across all processes)
        /// </summary>
        public void Dispose()
        {
            _stream.Dispose();
            try
            {
                File.Delete(_path);
            }
            catch
            {
                // ignore?
            }
        }
    }
}