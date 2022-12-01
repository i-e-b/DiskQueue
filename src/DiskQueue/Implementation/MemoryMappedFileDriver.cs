using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;

namespace DiskQueue.Implementation
{
    /// <summary>
    /// Uses the memory-mapped file interface to interact with file system.
    /// <p></p>
    /// All memory-mapped files are a fixed size while we hold them.
    /// They get allocated in chunks of an OS page size (usually 4K)
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
                for (var i = 0; i < RetryLimit; i++)
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
                return new MemoryMapStreamWrapper(path);
            }
        }

        public IFileStream OpenReadStream(string path)
        {
            lock (_lock)
            {
                if (!File.Exists(path)) File.WriteAllBytes(path, Array.Empty<byte>());
                return new MemoryMapStreamWrapper(path);
            }
        }

        public IFileStream OpenWriteStream(string dataFilePath)
        {
            lock (_lock)
            {
                if (!File.Exists(dataFilePath)) File.WriteAllBytes(dataFilePath, Array.Empty<byte>());
                return new MemoryMapStreamWrapper(dataFilePath);
            }
        }

        public void AtomicRead(string path, Action<IBinaryReader> action)
        {
            for (var i = 1; i <= RetryLimit; i++)
            {
                try
                {
                    AtomicReadInternal(path, action);
                    return;
                }
                catch (UnrecoverableException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Atomic read failed: {ex}");
                    if (i >= RetryLimit) throw;
                    Thread.Sleep(i * 100);
                }
            }
        }

        public void AtomicWrite(string path, Action<IBinaryWriter> action)
        {
            for (var i = 1; i <= RetryLimit; i++)
            {
                try
                {
                    AtomicWriteInternal(path, action);
                    return;
                }
                catch
                {
                    if (i >= RetryLimit) throw;
                    Thread.Sleep(i * 100);
                }
            }
        }
        
        /// <summary>
        /// Run a read action over a file by name.
        /// Access is optimised for sequential scanning.
        /// No file share is permitted.
        /// </summary>
        /// <param name="path">File path to read</param>
        /// <param name="action">Action to consume file stream. You do not need to close the stream yourself.</param>
        private void AtomicReadInternal(string path, Action<IBinaryReader> action)
        {
            lock (_lock)
            {
                if (FileExists(path + ".old_copy"))
                {
                    if (WaitDelete(path))
                        Move(path + ".old_copy", path);
                }

                using var stream = new MemoryMapStreamWrapper(path);
				
                SetPermissions.TryAllowReadWriteForAll(path);
                action(stream);
            }
        }
        

        /// <summary>
        /// Run a write action to a file.
        /// This will always rewrite the file (no appending).
        /// </summary>
        /// <param name="path">File path to write</param>
        /// <param name="action">Action to write into file stream. You do not need to close the stream yourself.</param>
        private void AtomicWriteInternal(string path, Action<IBinaryWriter> action)
        {
            lock (_lock)
            {
                // if the old copy file exists, this means that we have
                // a previous corrupt write, so we will not overwrite it, but 
                // rather overwrite the current file and keep it as our backup.
                if (FileExists(path) && !FileExists(path + ".old_copy"))
                    Move(path, path + ".old_copy");

                using var stream = new MemoryMapStreamWrapper(path);
				
                SetPermissions.TryAllowReadWriteForAll(path);
                action(stream);

                WaitDelete(path + ".old_copy");
            }
        }
        
        
        private bool WaitDelete(string s)
        {
            for (var i = 0; i < RetryLimit; i++)
            {
                try
                {
                    lock (_lock)
                    {
                        PrepareDelete(s);
                        Finalise();
                    }

                    return true;
                }
                catch
                {
                    Thread.Sleep(100);
                }
            }
            return false;
        }

        public bool FileExists(string path)
        {
            lock (_lock)
            {
                return File.Exists(path);
            }
        }

        public void DeleteRecursive(string path)
        {
            lock (_lock)
            {
                if (Path.GetPathRoot(path) == Path.GetFullPath(path)) throw new Exception("Request to delete root directory rejected");
                if (string.IsNullOrWhiteSpace(Path.GetDirectoryName(path)!)) throw new Exception("Request to delete root directory rejected");
                if (File.Exists(path)) throw new Exception("Tried to recursively delete a single file.");

                Directory.Delete(path, true);
            }
        }
    }

    /// <summary>
    /// Lock access to the file, and expose memory-mapping as growable stream methods
    /// </summary>
    internal class MemoryMapStreamWrapper : IFileStream, IBinaryReader, IBinaryWriter
    {
        private readonly string _path;
        private readonly object _lock = new();
        private long _position;
        
        public MemoryMapStreamWrapper(string path)
        {
            _path = path;
            _position = 0;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public long Write(byte[] bytes)
        {
            throw new NotImplementedException();
        }

        public void Truncate()
        {
            throw new NotImplementedException();
        }

        public Task<long> WriteAsync(byte[] bytes)
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

        public int ReadInt32()
        {
            throw new NotImplementedException();
        }

        public byte ReadByte()
        {
            throw new NotImplementedException();
        }

        public byte[] ReadBytes(int count)
        {
            throw new NotImplementedException();
        }

        public long GetLength()
        {
            throw new NotImplementedException();
        }

        public long GetPosition()
        {
            throw new NotImplementedException();
        }

        public long ReadInt64()
        {
            throw new NotImplementedException();
        }
    }
}