using System;
using System.Threading.Tasks;

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
        /// Write all bytes to a stream, returning new position
        /// </summary>
        Task<long> WriteAsync(byte[] bytes);

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
        long GetPosition();
    }
}