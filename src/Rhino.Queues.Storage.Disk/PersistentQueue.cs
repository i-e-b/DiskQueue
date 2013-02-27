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
			_queue.Dispose();
		}

		public IPersistentQueueSession OpenSession()
		{
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
	}
}