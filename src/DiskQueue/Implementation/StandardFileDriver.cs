using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace DiskQueue.Implementation
{
	/// <summary>
    /// A wrapper around System.IO.File to help with
    /// heavily multi-threaded and multi-process workflows
    /// </summary>
    internal class StandardFileDriver: IFileDriver
    {
	    public const int RetryLimit = 10;
	    
        private static readonly object _lock = new();
        private static readonly Queue<string> _waitingDeletes = new();
        
        public string GetFullPath(string path) => Path.GetFullPath(path);
        public string PathCombine(string a, string b) => Path.Combine(a,b);

        /// <summary>
        /// Test for the existence of a directory
        /// </summary>
        public bool DirectoryExists(string path)
        {
            lock (_lock)
            {
                return Directory.Exists(path);
            }
        }
        
        /// <summary>
        /// Moves a file to a temporary name and adds it to an internal
        /// delete list. Files are permanently deleted on a call to Finalise()
        /// </summary>
        public void PrepareDelete(string path)
        {
            lock (_lock)
            {
	            if (!FileExists(path)) return;
                var dir = Path.GetDirectoryName(path) ?? "";
                var file = Path.GetFileNameWithoutExtension(path);
                var prefix = Path.GetRandomFileName();
                
                var deletePath = Path.Combine(dir, $"{file}_dc_{prefix}");

                if (Move(path, deletePath))
                {
	                _waitingDeletes.Enqueue(deletePath);
                }
            }
        }

        /// <summary>
        /// Commit any pending prepared operations.
        /// Each operation will happen in serial.
        /// </summary>
        public void Finalise()
        {
            lock (_lock)
            {
                while (_waitingDeletes.Count > 0)
                {
                    var path = _waitingDeletes.Dequeue();
                    if (path is null) continue;
                    File.Delete(path);
                }
            }
        }

        /// <summary>
        /// Create and open a new file with no sharing between processes.
        /// </summary>
        private static ILockFile CreateNoShareFile(string path)
        {
            lock (_lock)
            {
                var lockStream = new FileStream(path,
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.None);
                
                return new LockFile(lockStream, path);
            }
        }

        /// <summary>
        /// Test for the existence of a file
        /// </summary>
        public bool FileExists(string path)
        {
            lock (_lock)
            {
                return File.Exists(path);
            }
        }

        public void DeleteRecursive(string path)
        {
	        if (Path.GetPathRoot(path) == Path.GetFullPath(path)) throw new Exception("Request to delete root directory rejected");
	        if (string.IsNullOrWhiteSpace(Path.GetDirectoryName(path)!)) throw new Exception("Request to delete root directory rejected");
	        if (File.Exists(path)) throw new Exception("Tried to recursively delete a single file.");
	        
	        Directory.Delete(path, true);
        }

        public Maybe<ILockFile> CreateLockFile(string path)
        {
	        try
	        {
		        return CreateNoShareFile(path).Success();
	        }
	        catch (Exception ex)
	        {
		        return Maybe<ILockFile>.Fail(ex);
	        }
        }
        
        public void ReleaseLock(ILockFile fileLock)
        {
	        lock (_lock)
	        {
		        fileLock.Dispose();
	        }
        }

        /// <summary>
        /// Attempt to create a directory. No error if the directory already exists.
        /// </summary>
        public void CreateDirectory(string path)
        {
            lock (_lock)
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Rename a file, including its path
        /// </summary>
        private static bool Move(string oldPath, string newPath)
        {
            lock (_lock)
            {
	            for (int i = 0; i < RetryLimit; i++)
	            {
		            try
		            {
			            File.Move(oldPath, newPath);
			            return true;
		            }
		            catch
		            {
			            Thread.Sleep(i * 100);
		            }
	            }
            }
            return false;
        }

        public IFileStream OpenTransactionLog(string path, int bufferLength)
        {
	        lock (_lock)
	        {
		        var stream = new FileStream(path,
			        FileMode.Append,
			        FileAccess.Write,
			        FileShare.None,
			        bufferLength,
			        FileOptions.SequentialScan | FileOptions.WriteThrough);

		        return new FileStreamWrapper(stream);
	        }
        }

        public IFileStream OpenReadStream(string path)
        {
	        lock (_lock)
	        {
		        var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
		        return new FileStreamWrapper(stream);
	        }
        }

        public IFileStream OpenWriteStream(string dataFilePath)
        {
	        lock (_lock)
	        {
		        var stream = new FileStream(
			        dataFilePath,
			        FileMode.OpenOrCreate,
			        FileAccess.Write,
			        FileShare.ReadWrite,
			        0x10000,
			        FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.WriteThrough);

		        SetPermissions.TryAllowReadWriteForAll(dataFilePath);
		        return new FileStreamWrapper(stream);
	        }
        }

        public void AtomicRead(string path, Action<IBinaryReader> action)
        {
	        for (int i = 1; i <= RetryLimit; i++)
	        {
		        try
		        {
			        AtomicReadInternal(path, fileStream =>
			        {
				        var wrapper = new FileStreamWrapper(fileStream);
				        action(wrapper);
			        });
			        return;
		        }
		        catch (UnrecoverableException)
		        {
			        throw;
		        }
		        catch (Exception ex)
		        {
			        Console.WriteLine($"Atomic read failed: {ex}");
			        if (i >= RetryLimit) throw;
			        Thread.Sleep(i * 100);
		        }
	        }
        }

        public void AtomicWrite(string path, Action<IBinaryWriter> action)
        {
	        for (int i = 1; i <= RetryLimit; i++)
	        {
		        try
		        {
			        AtomicWriteInternal(path, fileStream => { 
				        var wrapper = new FileStreamWrapper(fileStream);
				        action(wrapper);
			        });
			        return;
		        }
		        catch
		        {
			        if (i >= RetryLimit) throw;
			        Thread.Sleep(i * 100);
		        }
	        }
        }

        /// <summary>
		/// Run a read action over a file by name.
		/// Access is optimised for sequential scanning.
		/// No file share is permitted.
		/// </summary>
		/// <param name="path">File path to read</param>
		/// <param name="action">Action to consume file stream. You do not need to close the stream yourself.</param>
        private void AtomicReadInternal(string path, Action<FileStream> action)
		{
			lock (_lock)
			{
				if (FileExists(path + ".old_copy"))
				{
					if (WaitDelete(path))
						Move(path + ".old_copy", path);
				}

				using var stream = new FileStream(path,
					FileMode.OpenOrCreate,
					FileAccess.Read,
					FileShare.None,
					0x10000,
					FileOptions.SequentialScan);
				
				SetPermissions.TryAllowReadWriteForAll(path);
				action(stream);
			}
		}

		/// <summary>
		/// Run a write action to a file.
		/// This will always rewrite the file (no appending).
		/// </summary>
		/// <param name="path">File path to write</param>
		/// <param name="action">Action to write into file stream. You do not need to close the stream yourself.</param>
		private void AtomicWriteInternal(string path, Action<FileStream> action)
		{
			lock (_lock)
			{
				// if the old copy file exists, this means that we have
				// a previous corrupt write, so we will not overwrite it, but 
				// rather overwrite the current file and keep it as our backup.
				if (FileExists(path) && !FileExists(path + ".old_copy"))
					Move(path, path + ".old_copy");

				using var stream = new FileStream(path,
					FileMode.Create,
					FileAccess.Write,
					FileShare.None,
					0x10000,
					FileOptions.WriteThrough | FileOptions.SequentialScan);
				
				SetPermissions.TryAllowReadWriteForAll(path);
				action(stream);
				HardFlush(stream);

				WaitDelete(path + ".old_copy");
			}
		}

		/// <summary>
		/// Flush a stream, checking to see if its a file -- in which case it will ask for a flush-to-disk.
		/// </summary>
		private static void HardFlush(Stream? stream)
		{
			if (stream == null) return;
			if (stream is FileStream fs) fs.Flush(true);
			stream.Flush();
		}

		private bool WaitDelete(string s)
		{
			for (int i = 0; i < RetryLimit; i++)
			{
				try
				{
					lock (_lock)
					{
						PrepareDelete(s);
						Finalise();
					}

					return true;
				}
				catch
				{
					Thread.Sleep(100);
				}
			}
			return false;
		}
    }

	/// <summary>
	/// Strict mode exceptions that can't be retried
	/// </summary>
	public class UnrecoverableException : Exception
	{
		/// <summary>
		/// Create an unrecoverable exception with a message
		/// </summary>
		public UnrecoverableException(string msg):base (msg) { }
	}
}