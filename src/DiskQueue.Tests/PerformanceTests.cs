using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

// ReSharper disable PossibleNullReferenceException
// ReSharper disable AssignNullToNotNullAttribute

namespace DiskQueue.Tests
{
	[TestFixture, Explicit, SingleThreaded]
	public class PerformanceTests : PersistentQueueTestsBase
	{
		protected override string Path => "PerformanceTests";

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
		public void write_heavy_multi_thread_workload()
		{
			using (var queue = new PersistentQueue(Path)) { queue.HardDelete(false); }

			var rnd = new Random();
			var threads = new Thread[200];
			
			// enqueue threads
			for (int i = 0; i < 100; i++)
			{
				var j = i;
				threads[i] = new Thread(() => {
					for (int k = 0; k < 10; k++)
					{
						Thread.Sleep(rnd.Next(5));
						using (var q = PersistentQueue.WaitFor(Path, TimeSpan.FromSeconds(50)))
						{
							using var s = q.OpenSession();
							s.Enqueue(Encoding.ASCII.GetBytes($"Thread {j} enqueue {k}"));
							s.Flush();
						}
					}
				}){IsBackground = true};
				threads[i].Start();
			}
			
			// dequeue single
			Thread.Sleep(1000);
			var count = 0;
			while (true)
			{
				byte[]? bytes;
				using (var q = PersistentQueue.WaitFor(Path, TimeSpan.FromSeconds(50)))
				{
					using var s = q.OpenSession();

					bytes = s.Dequeue();
					s.Flush();
				}

				if (bytes is null) break;
				count++;
				Console.WriteLine(Encoding.ASCII.GetString(bytes));
			}
			Assert.That(count, Is.EqualTo(1000), "did not receive all messages");
		}
		
		
		[Test]
		public void read_heavy_multi_thread_workload()
		{
			using (var queue = new PersistentQueue(Path)) { queue.HardDelete(false); }
			
			// dequeue single
			new Thread(() =>
			{
				for (int i = 0; i < 1000; i++)
				{
					using var q = PersistentQueue.WaitFor(Path, TimeSpan.FromSeconds(50));
					using var s = q.OpenSession();
					s.Enqueue(Encoding.ASCII.GetBytes($"Enqueue {i}"));
					s.Flush();
				}
			}).Start();

			Thread.Sleep(1000);
			var rnd = new Random();
			var threads = new Thread[200];
			
			// dequeue threads
			for (int i = 0; i < 100; i++)
			{
				threads[i] = new Thread(() => {
					var count = 10;
					while (count > 0)
					{
						Thread.Sleep(rnd.Next(5));
						using (var q = PersistentQueue.WaitFor(Path, TimeSpan.FromSeconds(50)))
						{
							using var s = q.OpenSession();
							var data = s.Dequeue();
							if (data != null)
							{
								count--;
								Console.WriteLine(Encoding.ASCII.GetString(data));
							}

							s.Flush();
						}
					}
				}){IsBackground = true};
				threads[i].Start();
			}

			for (int e = 0; e < 100; e++)
			{
				if (!threads[e].Join(110_000)) Assert.Fail($"reader timeout on thread {e}");
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