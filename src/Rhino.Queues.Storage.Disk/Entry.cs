namespace Rhino.Queues.Storage.Disk
{
	using System;

	public class Entry : IEquatable<Entry>
	{
		public Entry(int fileNumber, int start, int length)
		{
			FileNumber = fileNumber;
			Start = start;
			Length = length;
		}

		public Entry(Operation operation)
			: this(operation.FileNumber, operation.Start, operation.Length)
		{
		}

		/// <summary>
		/// The actual data for this entry. 
		/// This only has value coming _out_ of the queue.
		/// </summary>
		public byte[] Data { get; set; }
		
		public int FileNumber { get; set; }
		public int Start { get; set; }
		public int Length { get; set; }

		public bool Equals(Entry obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.FileNumber == FileNumber && obj.Start == Start && obj.Length == Length;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return Equals(obj as Entry);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = FileNumber;
				result = (result * 397) ^ Start;
				result = (result * 397) ^ Length;
				return result;
			}
		}

		public static bool operator ==(Entry left, Entry right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(Entry left, Entry right)
		{
			return !Equals(left, right);
		}
	}
}