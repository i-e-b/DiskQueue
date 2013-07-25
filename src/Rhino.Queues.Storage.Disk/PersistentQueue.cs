using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using DiskQueue.Implementation;

namespace DiskQueue
{
	public class PersistentQueue : IPersistentQueue
	{
		PersistentQueueImpl _queue;

		public PersistentQueue(string storagePath)
		{
			_queue = new PersistentQueueImpl(storagePath);
		}

		public PersistentQueue(string storagePath, int maxSize)
		{
			_queue = new PersistentQueueImpl(storagePath, maxSize);
		}

		public void Dispose()
		{
			var local = Interlocked.Exchange(ref _queue, null);
			if (local == null) return;
			local.Dispose();
		}

		~PersistentQueue()
		{
			Dispose();
		}

		public IPersistentQueueSession OpenSession()
		{
			if (_queue == null) throw new Exception("This queue has been disposed");
			return _queue.OpenSession();
		}

		public int EstimatedCountOfItemsInQueue { get { return _queue.EstimatedCountOfItemsInQueue; } }
		public IPersistentQueueImpl Internals { get { return _queue; } }
		public int MaxFileSize { get { return _queue.MaxFileSize; } }

		public long SuggestedMaxTransactionLogSize
		{
			get { return _queue.SuggestedMaxTransactionLogSize; }
			set { _queue.SuggestedMaxTransactionLogSize = value; }
		}

		public bool TrimTransactionLogOnDispose
		{
			get { return _queue.TrimTransactionLogOnDispose; }
			set { _queue.TrimTransactionLogOnDispose = value; }
		}

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
					catch (Exception ex)
					{
						Console.WriteLine("Blocked by " + ex.GetType().Name + "; " + ex.Message);
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
	}
}