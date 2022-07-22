using System;
using DiskQueue.Implementation;

namespace DiskQueue
{
    /// <inheritdoc cref="IPersistentQueue{T}" />
    public class PersistentQueue<T> : PersistentQueue, IPersistentQueue<T>
    {
        /// <inheritdoc />
        public PersistentQueue(string storagePath)
        {
            Queue = new PersistentQueueImpl<T>(storagePath);
        }

        /// <inheritdoc />
        public PersistentQueue(string storagePath, int maxSize, bool throwOnConflict = true)
        {
            Queue = new PersistentQueueImpl<T>(storagePath, maxSize, throwOnConflict);
        }

        /// <summary>
        /// Open an read/write session
        /// </summary>
        public new IPersistentQueueSession<T> OpenSession()
        {
            if (Queue == null) throw new Exception("This queue has been disposed");
            return ((PersistentQueueImpl<T>)Queue).OpenSession();
        }
    }
}