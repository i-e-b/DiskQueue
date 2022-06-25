using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using DiskQueue.Implementation;

namespace DiskQueue
{
    /// <summary>
    /// Default persistent queue <see cref="IPersistentQueue"/>
    /// <para>This queue establishes exclusive use of the storage until it is disposed.</para>
    /// <para>If you wish to share the store between processes, you should use `PersistentQueue.WaitFor`.</para>
    /// <para>If you want to share the store between threads in one process, you may share the Persistent Queue and
    /// have each thread call `OpenSession` for itself.</para>
    /// </summary>
    public class PersistentQueue<T> : PersistentQueue, IPersistentQueue<T>
    {
        private PersistentQueueImpl<T>? _queue;

        /// <summary>
        /// Create or connect to a persistent store at the given storage path.
        /// <para>Throws UnauthorizedAccessException if you do not have read and write permissions.</para>
        /// <para>Throws InvalidOperationException if another instance is attached to the backing store.</para>
        /// </summary>
        public PersistentQueue(string storagePath)
        {
            _queue = new PersistentQueueImpl<T>(storagePath);
        }

        /// <summary>
        /// Create or connect to a persistent store at the given storage path.
        /// Uses specific maximum file size (files will be split if they exceed this size).
        /// <para>Throws UnauthorizedAccessException if you do not have read and write permissions.</para>
        /// <para>Throws InvalidOperationException if another instance is attached to the backing store.</para>
        /// If `throwOnConflict` is set to false, data corruption will be silently ignored. Use this only where uptime is more important than data integrity.
        /// </summary>
        public PersistentQueue(string storagePath, int maxSize, bool throwOnConflict = true)
        {
            _queue = new PersistentQueueImpl<T>(storagePath, maxSize, throwOnConflict);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public new IPersistentQueueSession<T> OpenSession()
        {
            if (_queue == null) throw new Exception("This queue has been disposed");
            return _queue.OpenSession();
        }
    }
}