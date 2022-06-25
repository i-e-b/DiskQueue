using DiskQueue.PublicInterfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiskQueue.Implementation
{
    internal class PersistentQueueImpl<T> : PersistentQueueImpl, IPersistentQueueImpl<T>
    {
        public PersistentQueueImpl(string path) : base(path) { }
        public PersistentQueueImpl(string path, int maxFileSize, bool throwOnConflict) : base(path, maxFileSize, throwOnConflict) { }

        public new IPersistentQueueSession<T> OpenSession()
        {
            return new PersistentQueueSession<T>(this, CreateWriter(), SuggestedWriteBuffer, FileTimeoutMilliseconds);
        }
    }
}
