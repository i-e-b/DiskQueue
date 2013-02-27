namespace DiskQueue.Implementation
{
	public enum OperationType : byte
	{
		Enqueue = 1,
		Dequeue = 2,
		Reinstate = 3
	}
}