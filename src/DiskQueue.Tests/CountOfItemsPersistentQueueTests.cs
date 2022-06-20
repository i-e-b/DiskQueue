using NUnit.Framework;
// ReSharper disable PossibleNullReferenceException

namespace DiskQueue.Tests
{
    [TestFixture, SingleThreaded]
	public class CountOfItemsPersistentQueueTests : PersistentQueueTestsBase
	{
        [Test]
		public void Can_get_count_from_queue()
		{
			using (var queue = new PersistentQueue(Path))
			{
				queue.HardDelete(true);
				Assert.AreEqual(0, queue.EstimatedCountOfItemsInQueue);
			}
		}

		[Test]
		public void Can_enter_items_and_get_count_of_items()
		{
			using (var queue = new PersistentQueue(Path))
			{
                queue.HardDelete(true);
				for (byte i = 0; i < 5; i++)
				{
					using (var session = queue.OpenSession())
					{
						session.Enqueue(new[] { i });
						session.Flush();
					}
				}
				Assert.AreEqual(5, queue.EstimatedCountOfItemsInQueue);
			}
		}


		[Test]
		public void Can_get_count_of_items_after_queue_restart()
		{
			using (var queue = new PersistentQueue(Path))
			{
                queue.HardDelete(true);
				for (byte i = 0; i < 5; i++)
				{
					using (var session = queue.OpenSession())
					{
						session.Enqueue(new[] { i });
						session.Flush();
					}
				}
			}

			using (var queue = new PersistentQueue(Path))
			{
				Assert.AreEqual(5, queue.EstimatedCountOfItemsInQueue);
			}
		}
	}
}