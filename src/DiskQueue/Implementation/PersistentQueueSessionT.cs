namespace DiskQueue.Implementation
{
    /// <inheritdoc cref="IPersistentQueueSession{T}"/>
    public class PersistentQueueSession<T> : PersistentQueueSession, IPersistentQueueSession<T>
    {
        /// <summary>
        /// This class performs the serialization of the object to be queued into a byte array suitable for queueing.
        /// It defaults to <see cref="DefaultSerializationStrategy{T}"/>, but you are free to implement your own and inject it in.
        /// </summary>
        public ISerializationStrategy<T> SerializationStrategy { get; set; } = new DefaultSerializationStrategy<T>();

        /// <inheritdoc cref="IPersistentQueueSession{T}"/>
        public PersistentQueueSession(IPersistentQueueImpl queue, IFileStream currentStream, int writeBufferSize, int timeoutLimit) : base(queue, currentStream,
            writeBufferSize, timeoutLimit)
        {
        }

        /// <inheritdoc cref="IPersistentQueueSession{T}"/>
        public new T? Dequeue()
        {
            byte[]? bytes = base.Dequeue();
            T? obj = SerializationStrategy.Deserialize(bytes);
            return obj;

        }

        /// <inheritdoc cref="IPersistentQueueSession{T}"/>
        public void Enqueue(T data)
        {
            byte[]? bytes = SerializationStrategy.Serialize(data);
            if (bytes != null)
            {
                Enqueue(bytes);
            }
        }
    }
}
