using NUnit.Framework;

namespace Rhino.Queues.Storage.Disk.Tests
{
	using System.IO;

	public class PersistentQueueTestsBase
	{
		protected const string path = @".\queue";

		[SetUp]
		public void Setup()
		{
			if (Directory.Exists(path))
				Directory.Delete(path, true);
			Directory.CreateDirectory(path);
		}

		/// <summary>
		/// This ensures that we release all files before we complete a test
		/// </summary>
		[TearDown]
		public void Teardown()
		{
			if (Directory.Exists(path))
				Directory.Delete(path, true);
			Directory.CreateDirectory(path);
		}
	}
}