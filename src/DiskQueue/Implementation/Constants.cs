using System;

namespace DiskQueue.Implementation
{
	/// <summary>
	/// Magic constants used in the disk queue
	/// </summary>
	public static class Constants
	{
		/// <summary> Operation end marker </summary>
		public static int OperationSeparator = 0x42FEBCA1;
		/// <summary> Bytes of operation end marker </summary>
		public static byte[] OperationSeparatorBytes = BitConverter.GetBytes(OperationSeparator);

		/// <summary> Start of transaction marker </summary>
		public static Guid StartTransactionSeparatorGuid 
			= new Guid("b75bfb12-93bb-42b6-acb1-a897239ea3a5");

		/// <summary> Bytes of the start of transaction marker </summary>
		public static byte[] StartTransactionSeparator
			= StartTransactionSeparatorGuid.ToByteArray();

		/// <summary> End of transaction marker </summary>
		public static Guid EndTransactionSeparatorGuid
		= new Guid("866c9705-4456-4e9d-b452-3146b3bfa4ce");

		/// <summary> Bytes of end of transaction marker </summary>
		public static byte[] EndTransactionSeparator
			= EndTransactionSeparatorGuid.ToByteArray();

	}
}