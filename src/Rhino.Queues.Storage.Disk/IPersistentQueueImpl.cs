namespace Rhino.Queues.Storage.Disk
{
	using System;
	using System.Collections.Generic;
	using System.IO;

	public delegate long WriterAcquiration(ref Stream stream);

	public interface IPersistentQueueImpl : IPersistentQueue
	{
		void AcquireWriter(Stream stream, Func<Stream, long> action, Action<Stream> onReplaceStream);
		void CommitTransaction(ICollection<Operation> operations);
		Entry Dequeue();
		void Reinstate(IEnumerable<Operation> reinstatedOperations);
		int CurrentFileNumber { get;  }
	}
}