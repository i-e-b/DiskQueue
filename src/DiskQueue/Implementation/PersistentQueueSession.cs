using System;
using System.Collections.Generic;
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
		private readonly List<Operation> _operations = new();
		private readonly List<Exception> _pendingWritesFailures = new();
		private readonly List<WaitHandle> _pendingWritesHandles = new();
		private IFileStream _currentStream;
		private readonly int _writeBufferSize;
		private readonly int _timeoutLimitMilliseconds;
		private readonly IPersistentQueueImpl _queue;
		private readonly List<IFileStream> _streamsToDisposeOnFlush = new();
		private volatile bool _disposed;

		private readonly List<byte[]> _buffer = new();
		private int _bufferSize;

		private const int MinSizeThatMakeAsyncWritePractical = 64 * 1024;

		/// <summary>
		/// Create a default persistent queue session.
		/// <para>You should use <see cref="IPersistentQueue.OpenSession"/> to get a session.</para>
		/// <example>using (var q = PersistentQueue.WaitFor("myQueue")) using (var session = q.OpenSession()) { ... }</example>
		/// </summary>
		public PersistentQueueSession(IPersistentQueueImpl queue, IFileStream currentStream, int writeBufferSize, int timeoutLimit)
		{
			_queue = queue;
			_currentStream = currentStream;
			if (writeBufferSize < MinSizeThatMakeAsyncWritePractical)
				writeBufferSize = MinSizeThatMakeAsyncWritePractical;
			_writeBufferSize = writeBufferSize;
			_timeoutLimitMilliseconds = timeoutLimit;
			_disposed = false;
		}

		/// <summary>
		/// Queue data for a later decode. Data is written on `Flush()`
		/// </summary>
		public void Enqueue(byte[] data)
		{
			_buffer.Add(data);
			_bufferSize += data.Length;
			if (_bufferSize > _writeBufferSize)
			{
				AsyncFlushBuffer();
			}
		}

		private void AsyncFlushBuffer()
		{
			_queue.AcquireWriter(_currentStream, AsyncWriteToStream, OnReplaceStream);
		}

		private void SyncFlushBuffer()
		{
			_queue.AcquireWriter(_currentStream, stream =>
			{
				var data = ConcatenateBufferAndAddIndividualOperations(stream);
				return stream.Write(data);
			}, OnReplaceStream);
		}

		// TODO: re-write this with modern async/await
		private long AsyncWriteToStream(IFileStream stream)
		{
			var data = ConcatenateBufferAndAddIndividualOperations(stream);
			var resetEvent = new ManualResetEvent(false);
			_pendingWritesHandles.Add(resetEvent);
			var positionAfterWrite = stream.GetPosition() + data.Length;
			try
			{
				positionAfterWrite = stream.Write(data);
			}
			catch (Exception e)
			{
				lock (_pendingWritesFailures)
				{
					_pendingWritesFailures.Add(e);
				}
			}
			
			return positionAfterWrite;
		}

		private byte[] ConcatenateBufferAndAddIndividualOperations(IFileStream stream)
		{
			var data = new byte[_bufferSize];
			var start = (int)stream.GetPosition();
			var index = 0;
			foreach (var bytes in _buffer)
			{
				_operations.Add(new Operation(
					OperationType.Enqueue,
					_queue.CurrentFileNumber,
					start,
					bytes.Length
				));
				Buffer.BlockCopy(bytes, 0, data, index, bytes.Length);
				start += bytes.Length;
				index += bytes.Length;
			}
			_bufferSize = 0;
			_buffer.Clear();
			return data;
		}

		private void OnReplaceStream(IFileStream newStream)
		{
			_streamsToDisposeOnFlush.Add(_currentStream);
			_currentStream = newStream;
		}

		/// <summary>
		/// Try to pull data from the queue. Data is removed from the queue on `Flush()`
		/// </summary>
		public byte[]? Dequeue()
		{
			var entry = _queue.Dequeue();
			if (entry == null)
				return null;
			_operations.Add(new Operation(
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
				foreach (var stream in _streamsToDisposeOnFlush)
				{
					stream.Flush();
					stream.Dispose();
				}
				_streamsToDisposeOnFlush.Clear();
			}
			_currentStream.Flush();
			_queue.CommitTransaction(_operations);
			_operations.Clear();
		}

		private void WaitForPendingWrites()
		{
			var timeoutCount = 0;
			while (_pendingWritesHandles.Count != 0)
			{
				var handles = _pendingWritesHandles.Take(64).ToArray();
				foreach (var handle in handles)
				{
					_pendingWritesHandles.Remove(handle);
				}
				
				var ok = WaitHandle.WaitAll(handles, _timeoutLimitMilliseconds);
				if (!ok) timeoutCount++;
				
				foreach (var handle in handles)
				{
					handle.Close();
				}
			}
			AssertNoPendingWritesFailures();
			if (timeoutCount > 0) throw new Exception("File system async operations are timing out");
		}

		private void AssertNoPendingWritesFailures()
		{
			lock (_pendingWritesFailures)
			{
				if (_pendingWritesFailures.Count == 0)
					return;

				var array = _pendingWritesFailures.ToArray();
				_pendingWritesFailures.Clear();
				throw new PendingWriteException(array);
			}
		}

		/// <summary>
		/// Close session, restoring any non-flushed operations
		/// </summary>
		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;
			_queue.Reinstate(_operations);
			_operations.Clear();
			foreach (var stream in _streamsToDisposeOnFlush)
			{
				stream.Dispose();
			}

			_currentStream.Dispose();
			GC.SuppressFinalize(this);
			Thread.Sleep(0);
		}

		/// <summary>
		/// Dispose queue on destructor. This is a safety-valve. You should ensure you
		/// dispose of sessions normally.
		/// </summary>
		~PersistentQueueSession()
		{
			if (_disposed) return;
			Dispose();
		}
	}
}