using System;

namespace DiskQueue
{
	public interface IPersistentQueueSession : IDisposable
	{
		void Enqueue(byte[] data);
		byte[] Dequeue();
		void Flush();
	}
}
