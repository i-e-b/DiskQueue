using NUnit.Framework;
using System;
using System.Collections.Generic;
// ReSharper disable PossibleNullReferenceException
// ReSharper disable AssignNullToNotNullAttribute

namespace DiskQueue.Tests
{
	[TestFixture, Explicit]
	public class PerformanceTests : PersistentQueueTestsBase
	{
		[Test, Description(
			"With a mid-range SSD, this is some 20x slower " +
			"than with a single flush (depends on disk speed)")]
		public void Enqueue_million_items_with_100_flushes()
		{
			using (var queue = new PersistentQueue(Path))
			{
				for (int i = 0; i < 100; i++)
				{
					using (var session = queue.OpenSession())
					{
						for (int j = 0; j < 10000; j++)
						{
							session.Enqueue(Guid.NewGuid().ToByteArray());
						}
						session.Flush();
					}
				}
			}
		}

		[Test]
		public void Enqueue_million_items_with_single_flush()
		{
			using (var queue = new PersistentQueue(Path))
			{
				using (var session = queue.OpenSession())
				{
					for (int i = 0; i < LargeCount; i++)
					{
						session.Enqueue(Guid.NewGuid().ToByteArray());
					}
					session.Flush();
				}
			}
		}

		[Test]
		public void Enqueue_and_dequeue_million_items_same_queue()
		{
			using (var queue = new PersistentQueue(Path))
			{
				using (var session = queue.OpenSession())
				{
					for (int i = 0; i < LargeCount; i++)
					{
						session.Enqueue(Guid.NewGuid().ToByteArray());
					}
					session.Flush();
				}
			
				using (var session = queue.OpenSession())
				{
					for (int i = 0; i < LargeCount; i++)
					{
						Ignore();
					}
					session.Flush();
				}
			}
		}

		private static void Ignore() { }

		[Test]
		public void Enqueue_and_dequeue_million_items_restart_queue()
		{
			using (var queue = new PersistentQueue(Path))
			{
				using (var session = queue.OpenSession())
				{
					for (int i = 0; i < LargeCount; i++)
					{
						session.Enqueue(Guid.NewGuid().ToByteArray());
					}
					session.Flush();
				}
			}

			using (var queue = new PersistentQueue(Path))
			{
				using (var session = queue.OpenSession())
				{
					for (int i = 0; i < LargeCount; i++)
					{
						Ignore();
					}
					session.Flush();
				}
			}
		}

		[Test]
		public void Enqueue_and_dequeue_large_items_with_restart_queue()
		{
			var random = new Random();
			var itemsSizes = new List<int>();
			using (var queue = new PersistentQueue(Path))
			{
				using (var session = queue.OpenSession())
				{
					for (int i = 0; i < SmallCount; i++)
					{
						var data = new byte[random.Next(1024 * 512, 1024 * 1024)];
						itemsSizes.Add(data.Length);
						session.Enqueue(data);
					}

					session.Flush();
				}
			}

			using (var queue = new PersistentQueue(Path))
			{
				using (var session = queue.OpenSession())
				{
					for (int i = 0; i < SmallCount; i++)
					{
						Assert.AreEqual(itemsSizes[i], session.Dequeue()?.Length ?? -1);
					}

					session.Flush();
				}
			}
		}

		private const int LargeCount = 1000000;
		private const int SmallCount = 500;

	}
}