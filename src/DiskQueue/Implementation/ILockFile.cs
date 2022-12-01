using System;

namespace DiskQueue.Implementation
{
    /// <summary>
    /// An inter-process lock based on a file.
    /// The lock is removed by disposing of the instance.
    /// </summary>
    public interface ILockFile : IDisposable { }
}