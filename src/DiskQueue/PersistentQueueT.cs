using System;
using DiskQueue.Implementation;

namespace DiskQueue
{
    /// <inheritdoc cref="IPersistentQueue{T}" />
    public class PersistentQueue<T> : PersistentQueue, IPersistentQueue<T>
    {
        private PersistentQueueImpl<T>? _queue;

        /// <inheritdoc />
        public PersistentQueue(string storagePath)
        {
            _queue = new PersistentQueueImpl<T>(storagePath);
        }

        /// <inheritdoc />
        public PersistentQueue(string storagePath, int maxSize, bool throwOnConflict = true)
        {
            _queue = new PersistentQueueImpl<T>(storagePath, maxSize, throwOnConflict);
        }

        /// <summary>
        /// Open an read/write session
        /// </summary>
        public new IPersistentQueueSession<T> OpenSession()
        {
            if (_queue == null) throw new Exception("This queue has been disposed");
            return _queue.OpenSession();
        }
    }
}