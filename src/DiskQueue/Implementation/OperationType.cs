namespace DiskQueue.Implementation
{
	/// <summary>
	/// Type of change applicable to a queue
	/// </summary>
	public enum OperationType : byte
	{
		/// <summary> Add new data to the queue </summary>
		Enqueue = 1,

		/// <summary> Retrieve and remove data from a queue </summary>
		Dequeue = 2,

		/// <summary> Revert a dequeue. Data will remain present on the queue </summary>
		Reinstate = 3
	}
}