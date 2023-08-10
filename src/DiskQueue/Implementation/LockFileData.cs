using System.Runtime.InteropServices;

namespace DiskQueue.Implementation
{
    /// <summary>
    /// struct file containing the data related to the lock file
    /// order of fields matters, please use correct attributes if you want to rearange them
    /// </summary>
    public struct LockFileData
    {
        /// <summary>
        /// PID of the locking process
        /// </summary>
        public int ProcessId;

        /// <summary>
        /// Thread ID from the locking process
        /// </summary>
        public int ThreadId;

        /// <summary>
        /// Process start time represented as unix offset in milliseconds
        /// </summary>
        public long ProcessStart;
    }
}
