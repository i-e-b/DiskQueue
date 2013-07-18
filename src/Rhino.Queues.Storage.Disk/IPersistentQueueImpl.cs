using System;
using System.Collections.Generic;
using System.IO;
using DiskQueue.Implementation;

namespace DiskQueue
{
	public delegate long WriterAcquiration(ref Stream stream);

	public interface IPersistentQueueImpl : IDisposable
	{
		void AcquireWriter(Stream stream, Func<Stream, long> action, Action<Stream> onReplaceStream);
		void CommitTransaction(ICollection<Operation> operations);
		Entry Dequeue();
		void Reinstate(IEnumerable<Operation> reinstatedOperations);
		int CurrentFileNumber { get;  }
		bool TrimTransactionLogOnDispose { get; set; }

		/// <summary>
		/// If true, each transaction commit will flush the transaction log.
		/// This is slow, but ensures the log is correct per transaction in the event of a hard termination (i.e. power failure)
		/// Defaults to true.
		/// </summary>
		bool ParanoidFlushing { get; set; }
	}
}