namespace DiskQueue
{
    /// <inheritdoc/>
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
