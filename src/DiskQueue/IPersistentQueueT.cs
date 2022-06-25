using System;

namespace DiskQueue
{
    /// <summary>
    /// A queue tied to a specific persistent storage backing.
    /// Enqueue and dequeue operations happen within sessions.
    /// <example>using (var session = q.OpenSession()) {...}</example>
    /// Queue should be disposed after use. This will NOT destroy the backing storage.
    /// </summary>
    public interface IPersistentQueue<T> : IPersistentQueue
    {
        /// <summary>
        /// Open an read/write session
        /// </summary>
        new IPersistentQueueSession<T> OpenSession();
    }
}