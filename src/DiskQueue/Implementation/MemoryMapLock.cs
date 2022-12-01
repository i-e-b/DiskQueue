using System.IO;
using System.IO.MemoryMappedFiles;

namespace DiskQueue.Implementation
{
    /// <summary>
    /// A file system based cross-process lock.
    /// This uses memory-mapped files.
    /// </summary>
    internal class MemoryMapLock : ILockFile
    {
        private readonly MemoryMappedFile _mapFile;
        private readonly MemoryMappedViewAccessor _accessor;
        private readonly string _path;

        public MemoryMapLock(MemoryMappedFile mapFile, MemoryMappedViewAccessor accessor, string path)
        {
            _mapFile = mapFile;
            _accessor = accessor;
            _path = path;
        }

        public void Dispose()
        {
            try
            {
                _accessor.Write(0L, 0);
                _accessor.Flush();
            } catch { /*ignore*/}
            _accessor.Dispose();
            _mapFile.Dispose();
            File.Delete(_path);
        }
    }
}