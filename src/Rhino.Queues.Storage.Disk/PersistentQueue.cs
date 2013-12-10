using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using DiskQueue.Implementation;

namespace DiskQueue
{
	/// <summary>
	/// Default persistent queue <see cref="IPersistentQueue"/>
	/// <para>This queue establishes exclusive use of the storage until it is disposed.</para>
	/// <para>If you wish to share the store between processes, you should use `PersistentQueue.<see cref="WaitFor"/>`.</para>
	/// <para>If you want to share the store between threads in one process, you may share the Persistent Queue and
	/// have each thread call `OpenSession` for itself.</para>
	/// </summary>
	public sealed class PersistentQueue : IPersistentQueue
	{
		PersistentQueueImpl _queue;

		/// <summary>
		/// Wait a maximum time to open an exclusive session.
		/// <para>If sharing storage between processes, the resulting queue should disposed
		/// as soon as possible.</para>
		/// <para>Throws a TimeoutException if the queue can't be locked in the specified time</para>
		/// </summary>
		/// <exception cref="TimeoutException"></exception>
		public static IPersistentQueue WaitFor(string storagePath, TimeSpan maxWait)
		{
			var sw = new Stopwatch();
			try
			{
				sw.Start();
				do
				{
					try
					{
						return new PersistentQueue(storagePath);
					}
					catch (DirectoryNotFoundException)
					{
						throw new Exception("Target storagePath does not exist or is not accessible");
					}
					catch (PlatformNotSupportedException ex)
					{
						Console.WriteLine("Blocked by " + ex.GetType().Name + "; " + ex.Message + "\r\n\r\n" + ex.StackTrace);
						throw;
					}
					catch
					{
						Thread.Sleep(50);
					}
				} while (sw.Elapsed < maxWait);
			}
			finally
			{
				sw.Stop();
			}
			throw new TimeoutException("Could not aquire a lock in the time specified");
		}

		/// <summary>
		/// Create or connect to a persistent store at the given storage path.
		/// <para>Throws UnauthorizedAccessException if you do not have read and write permissions.</para>
		/// <para>Throws InvalidOperationException if another instance is attached to the backing store.</para>
		/// </summary>
		public PersistentQueue(string storagePath)
		{
			_queue = new PersistentQueueImpl(storagePath);
		}
		
		/// <summary>
		/// Create or connect to a persistent store at the given storage path.
		/// Uses specific maximum file size (files will be split if they exceed this size).
		/// Throws UnauthorizedAccessException if you do not have read and write permissions.
		/// Throws InvalidOperationException if another instance is attached to the backing store.
		/// </summary>
		public PersistentQueue(string storagePath, int maxSize)
		{
			_queue = new PersistentQueueImpl(storagePath, maxSize);
		}

		/// <summary>
		/// Close this queue connection. Does not destroy flushed data.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_queue", Justification = "Disposed in an interlock")]
		public void Dispose()
		{
			var local = Interlocked.Exchange(ref _queue, null);
			if (local == null) return;
			local.Dispose();
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose of the queue connection on destruction.
		/// This is a safety valve. You should ensure you dispose
		/// of connections properly.
		/// </summary>
		~PersistentQueue()
		{
			if (_queue == null) return;
			Dispose();
		}

		/// <summary>
		/// Open an read/write session
		/// </summary>
		public IPersistentQueueSession OpenSession()
		{
			if (_queue == null) throw new Exception("This queue has been disposed");
			return _queue.OpenSession();
		}

		/// <summary>
		/// Returns the number of items in the queue, but does not include items added or removed
		/// in currently open sessions.
		/// </summary>
		public int EstimatedCountOfItemsInQueue { get { return _queue.EstimatedCountOfItemsInQueue; } }

		/// <summary>
		/// Internal adjustables. Use with caution. Read the source code.
		/// </summary>
		public IPersistentQueueImpl Internals { get { return _queue; } }

		/// <summary>
		/// Maximum size of files in queue. New files will be rolled-out if this is exceeded.
		/// (i.e. this is NOT the maximum size of the queue)
		/// </summary>
		public int MaxFileSize { get { return _queue.MaxFileSize; } }

		/// <summary>
		/// If the transaction log is near this size, it will be flushed and trimmed.
		/// If you set Internals.ParanoidFlushing, this value is ignored.
		/// </summary>
		public long SuggestedMaxTransactionLogSize
		{
			get { return _queue.SuggestedMaxTransactionLogSize; }
			set { _queue.SuggestedMaxTransactionLogSize = value; }
		}

		/// <summary>
		/// Defaults to true.
		/// If true, transactions will be flushed and trimmed on Dispose (makes dispose a bit slower)
		/// If false, transaction log will be left as-is on Dispose.
		/// </summary>
		public bool TrimTransactionLogOnDispose
		{
			get { return _queue.TrimTransactionLogOnDispose; }
			set { _queue.TrimTransactionLogOnDispose = value; }
		}
	}
}