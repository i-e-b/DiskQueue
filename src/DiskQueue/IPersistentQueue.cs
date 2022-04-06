using System;

namespace DiskQueue
{
	/// <summary>
	/// A queue tied to a specific persistent storage backing.
	/// Enqueue and dequeue operations happen within sessions.
	/// <example>using (var session = q.OpenSession()) {...}</example>
	/// Queue should be disposed after use. This will NOT destroy the backing storage.
	/// </summary>
	public interface IPersistentQueue : IDisposable
	{
		/// <summary>
		/// Open an read/write session
		/// </summary>
		IPersistentQueueSession OpenSession();

		/// <summary>
		/// Returns the number of items in the queue, but does not include items added or removed
		/// in currently open sessions.
		/// </summary>
		int EstimatedCountOfItemsInQueue { get; }

		/// <summary>
		/// Advanced adjustable settings. Use with caution. Read the source code.
		/// </summary>
		IPersistentQueueImpl Internals { get; }

		/// <summary>
		/// Maximum size of files in queue. New files will be rolled-out if this is exceeded.
		/// (i.e. this is NOT the maximum size of the queue)
		/// </summary>
		int MaxFileSize { get; }

		/// <summary>
		/// WARNING: Dangerous!
		/// Attempt to delete the queue, all its data, and all support files.
		/// This is not thread safe, multi-process safe, or safe in any other way.
		///<para></para>
		/// If reset is true, the queue's directory and lock file will be restored, and the queue can continue to be used.
		/// </summary>
		void HardDelete(bool reset);
	}
}