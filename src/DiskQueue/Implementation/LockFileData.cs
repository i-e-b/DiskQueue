using System.Runtime.InteropServices;

namespace DiskQueue.Implementation
{
    /// <summary>
    /// Data related to the lock file.
    /// This is written to the lock file on disk, so order of fields matters,
    /// Rearranging these will result in incompatible lock files.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
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
