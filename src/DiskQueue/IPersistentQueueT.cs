namespace DiskQueue
{

    /// <inheritdoc />
    public interface IPersistentQueue<T> : IPersistentQueue
    {
        /// <summary>
        /// Open an read/write session
        /// </summary>
        new IPersistentQueueSession<T> OpenSession();
    }
}