namespace DiskQueue.Implementation
{
    /// <inheritdoc cref="IPersistentQueueSession{T}"/>
    public class PersistentQueueSession<T> : PersistentQueueSession, IPersistentQueueSession<T>
    {
        /// <inheritdoc cref="IPersistentQueueSession{T}"/>
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
