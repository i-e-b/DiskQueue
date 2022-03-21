using System;
using DiskQueue.Implementation;

namespace DiskQueue
{
    /// <summary>
    /// Wrapper for file activity
    /// </summary>
    public interface IFileStream : IDisposable
    {
        /// <summary>
        /// Write all bytes to a stream, returning new position
        /// </summary>
        long Write(byte[] bytes);

        /// <summary>
        /// Flush bytes from buffers to storage
        /// </summary>
        void Flush();

        /// <summary>
        /// Move to a byte offset from the start of the stream
        /// </summary>
        void MoveTo(long offset);

        /// <summary>
        /// Read from stream into buffer, returning number of bytes actually read.
        /// If the underlying stream supplies no bytes, this adaptor should try until a timeout is reached.
        /// An exception will be thrown if the file returns no bytes within the timeout window.
        /// </summary>
        int Read(byte[] buffer, int offset, int length); //"End of file reached while trying to read queue item"

        /// <summary>
        /// Return a binary reader for the given file stream
        /// </summary>
        /// <returns></returns>
        IBinaryReader GetBinaryReader();

        /// <summary>
        /// Extend the underlying stream to the given length
        /// </summary>
        void SetLength(long length);

        /// <summary>
        /// Set the read/write position of the underlying file
        /// </summary>
        void SetPosition(long position);

        /// <summary>
        /// Get the current read/write position of the underlying file
        /// </summary>
        int GetPosition();
    }

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
        Maybe<object> CreateLockFile(string path);

        /// <summary>
        /// Release a lock that was previously held
        /// </summary>
        void ReleaseLock(object fileLock);

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
    }
}