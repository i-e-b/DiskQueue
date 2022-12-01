using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace DiskQueue.Implementation
{
    /// <summary>
    /// Uses the memory-mapped file interface to interact with file system
    /// </summary>
    internal class MemoryMappedFileDriver : IFileDriver
    {
        public const int RetryLimit = 10;
	    
        private static readonly object _lock = new();
        private readonly Queue<string> _waitingDeletes = new();
        
        public string GetFullPath(string path) => Path.GetFullPath(path);
        public string PathCombine(string a, string b) => Path.Combine(a,b);

        public bool DirectoryExists(string path)
        {
            lock (_lock)
            {
                return Directory.Exists(path);
            }
        }

        /// <summary>
        /// We try to BOTH get a file lock AND check its contents.
        /// </summary>
        public Maybe<ILockFile> CreateLockFile(string path)
        {
            MemoryMappedFile? mapFile = null;
            MemoryMappedViewAccessor? accessor = null;
            try
            {
                ILockFile ret;
                lock (_lock)
                {
                    var thisProcess = Process.GetCurrentProcess().Id;
                    var key = Identify.Thread();
                    var keyBytes = BitConverter.GetBytes(key);
                    
                    if (!File.Exists(path)) File.WriteAllBytes(path, keyBytes);
                    
                    mapFile = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null!, 8);
                    accessor = mapFile.CreateViewAccessor(offset: 0, size: 8);
                    
                    var current = accessor.ReadInt64(0);
                    if (current == 0)
                    {
                        accessor.Write(key, 0);
                    }

                    current = accessor.ReadInt64(0);
                    if (current != key)
                    {
                        // The first two *should not* happen, but filesystems seem to have weird bugs.
                        // Is this for a current process?
                        var lockingProcess = (int)(key & 0xFFFF_FFFF);
                        if (lockingProcess == thisProcess)
                        {
                            var threadId = (int)(key >> 32);
                            throw new Exception($"This queue is locked by another thread in this process. Thread id = {threadId}");
                        }

                        if (IsRunning(lockingProcess))
                        {
                            throw new Exception($"This queue is locked by another running process. Process id = {lockingProcess}");
                        }

                        // We have a lock from a dead process. Kill it.
                        File.Delete(path);
                        File.WriteAllBytes(path, keyBytes);
                        
                        mapFile = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null!, 8);
                        accessor = mapFile.CreateViewAccessor(offset: 0, size: 8);
                    }

                    ret = new MemoryMapLock(mapFile, accessor, path);
                }
                return ret.Success();
            }
            catch (Exception ex)
            {
                accessor?.Dispose();
                mapFile?.Dispose();
                return Maybe<ILockFile>.Fail(ex);
            }
        }

        /// <summary>
        /// Return true if the processId matches a running process
        /// </summary>
        private static bool IsRunning(int processId)
        {
            try
            {
                Process.GetProcessById(processId);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch
            {
                return true;
            }
        }

        public void ReleaseLock(ILockFile fileLock)
        {
            lock (_lock)
            {
                fileLock.Dispose();
            }
        }

        public void PrepareDelete(string path)
        {
            lock (_lock)
            {
                if (!FileExists(path)) return;
                var dir = Path.GetDirectoryName(path) ?? "";
                var file = Path.GetFileNameWithoutExtension(path);
                var prefix = Path.GetRandomFileName();
                
                var deletePath = Path.Combine(dir, $"{file}_dc_{prefix}");

                if (Move(path, deletePath))
                {
                    _waitingDeletes.Enqueue(deletePath);
                }
            }
        }
        
        private static bool Move(string oldPath, string newPath)
        {
            lock (_lock)
            {
                for (int i = 0; i < RetryLimit; i++)
                {
                    try
                    {
                        File.Move(oldPath, newPath);
                        return true;
                    }
                    catch
                    {
                        Thread.Sleep(i * 100);
                    }
                }
            }
            return false;
        }

        public void Finalise()
        {
            lock (_lock)
            {
                while (_waitingDeletes.Count > 0)
                {
                    var path = _waitingDeletes.Dequeue();
                    if (path is null) continue;
                    File.Delete(path);
                }
            }
        }

        public void CreateDirectory(string path)
        {
            lock (_lock)
            {
                Directory.CreateDirectory(path);
            }
        }

        public IFileStream OpenTransactionLog(string path, int bufferLength)
        {
            lock (_lock)
            {
                if (!File.Exists(path)) File.WriteAllBytes(path, Array.Empty<byte>());
                
                var mapFile = MemoryMappedFile.CreateFromFile(path, FileMode.Open);
                var stream = mapFile.CreateViewStream();
                return new FileStreamWrapper(stream);
            }
        }

        public IFileStream OpenReadStream(string path)
        {
            throw new NotImplementedException();
        }

        public IFileStream OpenWriteStream(string dataFilePath)
        {
            throw new NotImplementedException();
        }

        public void AtomicRead(string path, Action<IBinaryReader> action)
        {
            throw new NotImplementedException();
        }

        public void AtomicWrite(string path, Action<IBinaryWriter> action)
        {
            throw new NotImplementedException();
        }

        public bool FileExists(string path)
        {
            throw new NotImplementedException();
        }

        public void DeleteRecursive(string path)
        {
            throw new NotImplementedException();
        }
    }
}