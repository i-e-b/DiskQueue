using System;

namespace DiskQueue
{
    /// <summary>
    /// Queue session (exclusive use of the queue to add or remove items)
    /// The queue session should be wrapped in a `using`, as it must be disposed.
    /// If you are sharing access, you should hold the queue session for as little time as possible.
    /// </summary>
    public interface IPersistentQueueSession<T> : IPersistentQueueSession
    {
        /// <summary>
        /// Queue data for a later decode. Data is written on `Flush()`
        /// </summary>
        void Enqueue(T data);

        /// <summary>
        /// Try to pull data from the queue. Data is removed from the queue on `Flush()`
        /// </summary>
        new T? Dequeue();
    }
}
