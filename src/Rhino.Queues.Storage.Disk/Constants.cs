namespace Rhino.Queues.Storage.Disk
{
	using System;

	public static class Constants
	{
		public static int OperationSeparator = 0x42FEBCA1;
		public static byte[] OperationSeparatorBytes = BitConverter.GetBytes(OperationSeparator);

		public static Guid StartTransactionSeparatorGuid 
			= new Guid("b75bfb12-93bb-42b6-acb1-a897239ea3a5");

		public static byte[] StartTransactionSeparator
			= StartTransactionSeparatorGuid.ToByteArray();

		public static Guid EndTransactionSeparatorGuid
		= new Guid("866c9705-4456-4e9d-b452-3146b3bfa4ce");

		public static byte[] EndTransactionSeparator
			= EndTransactionSeparatorGuid.ToByteArray();

	}
}