namespace Rhino.Queues.Storage.Disk
{
	public class Operation
	{
		public Operation(OperationType type, int fileNumber, int start, int length)
		{
			Type = type;
			FileNumber = fileNumber;
			Start = start;
			Length = length;
		}

		public OperationType Type { get; set; }
		public int FileNumber { get; set; }
		public int Start { get; set; }
		public int Length { get; set; }
	}
}