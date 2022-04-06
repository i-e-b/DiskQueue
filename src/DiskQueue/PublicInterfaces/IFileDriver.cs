using System;
using DiskQueue.Implementation;

namespace DiskQueue
{
    /// <summary>
    /// Wrapper around BinaryReader
    /// </summary>
    public interface IBinaryReader:IDisposable
    {
        /// <summary>
        /// Read an Int32 from the current stream position
        /// </summary>
        int ReadInt32();

        /// <summary>
        /// Read a byte from the current stream position
        /// </summary>
        byte ReadByte();

        /// <summary>
        /// Read a number of bytes
        /// </summary>
        byte[] ReadBytes(int count);

        /// <summary>
        /// Read length of underlying stream
        /// </summary>
        long GetLength();

        /// <summary>
        /// Get current position in underlying stream
        /// </summary>
        long GetPosition();

        /// <summary>
        /// Read an Int64 from the current stream position
        /// </summary>
        long ReadInt64();
    }

    /// <summary>
    /// Interface for file access mechanism. For test and advanced users only.
    /// See the source code for more details.
    /// </summary>
    public interface IFileDriver
    {
        /// <summary>
        /// Proxy for Path.GetFullPath()
        /// </summary>
        string GetFullPath(string path);

        /// <summary>
        /// Proxy for Directory.Exists
        /// </summary>
        bool DirectoryExists(string path);

        /// <summary>
        /// Proxy for Path.Combine
        /// </summary>
        string PathCombine(string a, string b);

        /// <summary>
        /// Try to get a lock on a file path
        /// </summary>
        Maybe<LockFile> CreateLockFile(string path);

        /// <summary>
        /// Release a lock that was previously held
        /// </summary>
        void ReleaseLock(LockFile fileLock);

        /// <summary>
        /// Ready a file for delete on next call to Finalise
        /// </summary>
        /// <param name="path"></param>
        void PrepareDelete(string path);

        /// <summary>
        /// Complete any waiting file operations
        /// </summary>
        void Finalise();

        /// <summary>
        /// Proxy for Directory.Create
        /// </summary>
        /// <param name="path"></param>
        void CreateDirectory(string path);

        /// <summary>
        /// Open a transaction log file as a stream
        /// </summary>
        IFileStream OpenTransactionLog(string path, int bufferLength);

        /// <summary>
        /// Open a data file for reading
        /// </summary>
        IFileStream OpenReadStream(string path);

        /// <summary>
        /// Open a data file for writing
        /// </summary>
        IFileStream OpenWriteStream(string dataFilePath);
        
        /// <summary>
        /// Run a read action over a file by name.
        /// Access is optimised for sequential scanning.
        /// No file share is permitted.
        /// </summary>
        void AtomicRead(string path, Action<IFileStream> action);

        /// <summary>
        /// Run a write action over a file by name.
        /// No file share is permitted.
        /// </summary>
        void AtomicWrite(string path, Action<IFileStream> action);

        /// <summary>
        /// Returns true if a readable file exists at the given path. False otherwise
        /// </summary>
        bool FileExists(string path);
    }
}