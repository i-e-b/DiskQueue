using DiskQueue.Implementation;

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

        /// <summary>
        /// This class performs the serialization of the object to be queued into a byte array suitable for queueing.
        /// It defaults to <see cref="DefaultSerializationStrategy{T}"/>, but you are free to implement your own and inject it in.
        /// </summary>
        ISerializationStrategy<T> SerializationStrategy { get; set; }
    }
}
