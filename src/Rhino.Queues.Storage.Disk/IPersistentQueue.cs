namespace Rhino.Queues.Storage.Disk
{
	using System;

	public interface IPersistentQueue : IDisposable
	{
		PersistentQueueSession OpenSession();
	}
}