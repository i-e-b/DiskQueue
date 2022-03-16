using System.Collections.Generic;
using System.IO;

namespace DiskQueue.Implementation
{
    /// <summary>
    /// A wrapper around System.IO.File to help with
    /// heavily multi-threaded and multi-process workflows
    /// </summary>
    internal static class FileOperations
    {
        private static readonly object _lock = new();
        private static readonly Queue<string> _waitingDeletes = new();
        
        /// <summary>
        /// Moves a file to a temporary name and adds it to an internal
        /// delete list. Files are permanently deleted on a call to Finalise()
        /// </summary>
        public static void PrepareDelete(string path)
        {
            lock (_lock)
            {
                var dir = Path.GetDirectoryName(path) ?? "";
                var file = Path.GetFileNameWithoutExtension(path);
                var prefix = Path.GetRandomFileName();
                
                var deletePath = Path.Combine(dir, $"{file}_dc_{prefix}");
                
                File.Move(path, deletePath);
                _waitingDeletes.Enqueue(deletePath);
            }
        }

        /// <summary>
        /// Commit any pending prepared operations.
        /// Each operation will happen in serial.
        /// </summary>
        public static void Finalise()
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

        /// <summary>
        /// Create and open a new file with no sharing between processes.
        /// </summary>
        public static FileStream CreateNoShareFile(string path)
        {
            lock (_lock)
            {
                return new FileStream(path,
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.None);
            }
        }

        /// <summary>
        /// Test for the existence of a file
        /// </summary>
        public static bool FileExists(string path)
        {
            lock (_lock)
            {
                return File.Exists(path);
            }
        }

        /// <summary>
        /// Test for the existence of a directory
        /// </summary>
        public static bool DirectoryExists(string path)
        {
            lock (_lock)
            {
                return Directory.Exists(path);
            }
        }

        /// <summary>
        /// Attempt to create a directory. No error if the directory already exists.
        /// </summary>
        public static void CreateDirectory(string path)
        {
            lock (_lock)
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Rename a file, including its path
        /// </summary>
        public static void Move(string oldPath, string newPath)
        {
            lock (_lock)
            {
                File.Move(oldPath, newPath);
            }
        }
    }
}