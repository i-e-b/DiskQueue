using System;

namespace DiskQueue
{
	public interface IPersistentQueue : IDisposable
	{
		IPersistentQueueSession OpenSession();
		int EstimatedCountOfItemsInQueue { get; }
		IPersistentQueueImpl Internals { get; }
		int MaxFileSize { get; }
	}
}