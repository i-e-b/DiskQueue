using System;
using System.Collections.Generic;
using System.IO;
using DiskQueue.Implementation;

namespace DiskQueue
{
	/// <summary>
	/// Wrapper for exposing some inner workings of the persistent queue.
	/// <para>You should be careful using any of these methods</para>
	/// <para>Please read the source code before using these methods in production software</para>
	/// </summary>
	public interface IPersistentQueueImpl : IDisposable
	{
		/// <summary>
		/// <para>UNSAFE. Incorrect use will result in data loss.</para>
		/// Lock and process a data file writer at the current write head.
		/// <para>This will create new files if max size is exceeded</para>
		/// </summary>
		/// <param name="stream">Stream to write</param>
		/// <param name="action">Writing action</param>
		/// <param name="onReplaceStream">Continuation action if a new file is created</param>
		void AcquireWriter(Stream stream, Func<Stream, long> action, Action<Stream> onReplaceStream);

		/// <summary>
		/// <para>UNSAFE. Incorrect use will result in data loss.</para>
		/// Commit a sequence of operations to storage
		/// </summary>
		void CommitTransaction(ICollection<Operation> operations);

		/// <summary>
		/// <para>UNSAFE. Incorrect use will result in data loss.</para>
		/// Dequeue data, returning storage entry
		/// </summary>
		Entry Dequeue();

		/// <summary>
		/// <para>UNSAFE. Incorrect use will result in data loss.</para>
		/// <para>Undo Enqueue and Dequeue operations.</para>
		/// <para>These MUST have been real operations taken.</para>
		/// </summary>
		void Reinstate(IEnumerable<Operation> reinstatedOperations);

		/// <summary>
		/// <para>Safe, available for tests and performance.</para>
		/// <para>Current writing file number</para>
		/// </summary>
		int CurrentFileNumber { get; }

		/// <summary>
		/// <para>Safe, available for tests and performance.</para>
		/// <para>If true, trim and flush waiting transactions on dispose</para>
		/// </summary>
		bool TrimTransactionLogOnDispose { get; set; }

		/// <summary>
		/// <para>Setting this to false may cause unexpected data loss in some failure conditions.</para>
		/// <para>Defaults to true.</para>
		/// <para>If true, each transaction commit will flush the transaction log.</para>
		/// <para>This is slow, but ensures the log is correct per transaction in the event of a hard termination (i.e. power failure)</para>
		/// </summary>
		bool ParanoidFlushing { get; set; }
	}
}