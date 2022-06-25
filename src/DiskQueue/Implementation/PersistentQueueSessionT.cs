using System;
using System.Collections.Generic;
using System.Text;

namespace DiskQueue.Implementation
{
    internal class PersistentQueueSession<T> : PersistentQueueSession, IPersistentQueueSession<T>
    {
        public PersistentQueueSession(IPersistentQueueImpl queue, IFileStream currentStream, int writeBufferSize, int timeoutLimit) : base(queue, currentStream, writeBufferSize, timeoutLimit)
        {
        }

        public new T? Dequeue()
        {
            throw new NotImplementedException();
        }
        public void Enqueue(T data)
        {
            throw new NotImplementedException();
        }
    }
}
