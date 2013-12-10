namespace DiskQueue.Implementation
{
	/// <summary>
	/// Represents an operation on a specific file, with location and span.
	/// </summary>
	public class Operation
	{
		/// <summary>
		/// Create a new operation specification
		/// </summary>
		public Operation(OperationType type, int fileNumber, int start, int length)
		{
			Type = type;
			FileNumber = fileNumber;
			Start = start;
			Length = length;
		}

		/// <summary> Operation type </summary>
		public OperationType Type { get; set; }

		/// <summary> File number in the persistent queue file set </summary>
		public int FileNumber { get; set; }

		/// <summary> Offset within the file </summary>
		public int Start { get; set; }

		/// <summary> Length of data </summary>
		public int Length { get; set; }
	}
}