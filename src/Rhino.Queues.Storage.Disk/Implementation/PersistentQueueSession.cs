using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace DiskQueue.Implementation
{
	/// <summary>
	/// Default persistent queue session.
	/// <para>You should use <see cref="IPersistentQueue.OpenSession"/> to get a session.</para>
	/// <example>using (var q = PersistentQueue.WaitFor("myQueue")) using (var session = q.OpenSession()) { ... }</example>
	/// </summary>
	public sealed class PersistentQueueSession : IPersistentQueueSession
	{
		private readonly List<Operation> operations = new List<Operation>();
		private readonly IList<Exception> pendingWritesFailures = new List<Exception>();
		private readonly IList<WaitHandle> pendingWritesHandles = new List<WaitHandle>();
		private Stream currentStream;
		private readonly int writeBufferSize;
		private readonly IPersistentQueueImpl queue;
		private readonly List<Stream> streamsToDisposeOnFlush = new List<Stream>();
		static readonly object _ctorLock = new object();
		volatile bool disposed;

		private readonly List<byte[]> buffer = new List<byte[]>();
		private int bufferSize;

		private const int MinSizeThatMakeAsyncWritePractical = 64 * 1024;

		/// <summary>
		/// Create a default persistent queue session.
		/// <para>You should use <see cref="IPersistentQueue.OpenSession"/> to get a session.</para>
		/// <example>using (var q = PersistentQueue.WaitFor("myQueue")) using (var session = q.OpenSession()) { ... }</example>
		/// </summary>
		public PersistentQueueSession(IPersistentQueueImpl queue, Stream currentStream, int writeBufferSize)
		{
			lock (_ctorLock)
			{
				this.queue = queue;
				this.currentStream = currentStream;
				if (writeBufferSize < MinSizeThatMakeAsyncWritePractical)
					writeBufferSize = MinSizeThatMakeAsyncWritePractical;
				this.writeBufferSize = writeBufferSize;
				disposed = false;
			}
		}

		/// <summary>
		/// Queue data for a later decode. Data is written on `Flush()`
		/// </summary>
		public void Enqueue(byte[] data)
		{
			buffer.Add(data);
			bufferSize += data.Length;
			if (bufferSize > writeBufferSize)
			{
				AsyncFlushBuffer();
			}
		}

		private void AsyncFlushBuffer()
		{
			queue.AcquireWriter(currentStream, AsyncWriteToStream, OnReplaceStream);
		}

		private void SyncFlushBuffer()
		{
			queue.AcquireWriter(currentStream, stream =>
			{
				byte[] data = ConcatenateBufferAndAddIndividualOperations(stream);
				stream.Write(data, 0, data.Length);
				return stream.Position;
			}, OnReplaceStream);
		}

		private long AsyncWriteToStream(Stream stream)
		{
			byte[] data = ConcatenateBufferAndAddIndividualOperations(stream);
			var resetEvent = new ManualResetEvent(false);
			pendingWritesHandles.Add(resetEvent);
			long positionAfterWrite = stream.Position + data.Length;
			stream.BeginWrite(data, 0, data.Length, delegate(IAsyncResult ar)
			{
				try
				{
					stream.EndWrite(ar);
				}
				catch (Exception e)
				{
					lock (pendingWritesFailures)
					{
						pendingWritesFailures.Add(e);
					}
				}
				finally
				{
					resetEvent.Set();
				}
			}, null);
			return positionAfterWrite;
		}

		private byte[] ConcatenateBufferAndAddIndividualOperations(Stream stream)
		{
			var data = new byte[bufferSize];
			var start = (int)stream.Position;
			var index = 0;
			foreach (var bytes in buffer)
			{
				operations.Add(new Operation(
					OperationType.Enqueue,
					queue.CurrentFileNumber,
					start,
					bytes.Length
				));
				Buffer.BlockCopy(bytes, 0, data, index, bytes.Length);
				start += bytes.Length;
				index += bytes.Length;
			}
			bufferSize = 0;
			buffer.Clear();
			return data;
		}

		private void OnReplaceStream(Stream newStream)
		{
			streamsToDisposeOnFlush.Add(currentStream);
			currentStream = newStream;
		}

		/// <summary>
		/// Try to pull data from the queue. Data is removed from the queue on `Flush()`
		/// </summary>
		public byte[] Dequeue()
		{
			var entry = queue.Dequeue();
			if (entry == null)
				return null;
			operations.Add(new Operation(
				OperationType.Dequeue,
				entry.FileNumber,
				entry.Start,
				entry.Length
			));
			return entry.Data;
		}

		/// <summary>
		/// Commit actions taken in this session since last flush.
		/// If the session is disposed with no flush, actions are not persisted 
		/// to the queue (Enqueues are not written, dequeues are left on the queue)
		/// </summary>
		public void Flush()
		{
			try
			{
				WaitForPendingWrites();
				SyncFlushBuffer();
			}
			finally
			{
				foreach (var stream in streamsToDisposeOnFlush)
				{
					stream.Flush();
					stream.Dispose();
				}
				streamsToDisposeOnFlush.Clear();
			}
			currentStream.Flush();
			queue.CommitTransaction(operations);
			operations.Clear();
		}

		private void WaitForPendingWrites()
		{
			while (pendingWritesHandles.Count != 0)
			{
				var handles = pendingWritesHandles.Take(64).ToArray();
				foreach (var handle in handles)
				{
					pendingWritesHandles.Remove(handle);
				}
				WaitHandle.WaitAll(handles);
				foreach (var handle in handles)
				{
					handle.Close();
				}
				AssertNoPendingWritesFailures();
			}
		}

		private void AssertNoPendingWritesFailures()
		{
			lock (pendingWritesFailures)
			{
				if (pendingWritesFailures.Count == 0)
					return;

				var array = pendingWritesFailures.ToArray();
				pendingWritesFailures.Clear();
				throw new PendingWriteException(array);
			}
		}

		/// <summary>
		/// Close session, restoring any non-flushed operations
		/// </summary>
		public void Dispose()
		{
			lock (_ctorLock)
			{
				if (disposed) return;
				disposed = true;
				queue.Reinstate(operations);
				operations.Clear();
				foreach (var stream in streamsToDisposeOnFlush)
				{
					stream.Dispose();
				}
				currentStream.Dispose();
				GC.SuppressFinalize(this);
			}
			Thread.Sleep(0);
		}

		/// <summary>
		/// Dispose queue on destructor. This is a safty-valve. You should ensure you
		/// dispose of sessions normally.
		/// </summary>
		~PersistentQueueSession()
		{
			if (disposed) return;
			Dispose();
		}
	}
}