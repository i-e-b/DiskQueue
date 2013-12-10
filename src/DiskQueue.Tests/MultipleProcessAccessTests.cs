using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace DiskQueue.Tests
{
	[TestFixture]
	public class MultipleProcessAccessTests
	{
		[Test,
		Description("Multiple PersistentQueue instances are " +
		            "pretty much the same as multiple processes to " +
		            "the DiskQueue library")]
		public void Can_access_from_multiple_queues_if_used_carefully ()
		{
			var received = new List<byte[]>();
			int numberOfItems = 10;

			var t1 = new Thread(() => {
				for (int i = 0; i < numberOfItems; i++)
				{
					AddToQueue(new byte[] { 1, 2, 3 });
				}
			});
			var t2 = new Thread(() => {
				while (received.Count < numberOfItems)
				{
					var data = ReadQueue();
					if (data != null) received.Add(data);
				}
			});

			t1.Start();
			t2.Start();

			t1.Join();

			var ok = t2.Join(TimeSpan.FromSeconds(25));

			if (!ok)
			{
				t2.Abort();
				Assert.Fail("Did not receive all data in time");
			}
			Assert.That(received.Count, Is.EqualTo(numberOfItems), "received items");
		}

		void AddToQueue(byte[] data)
		{
			Thread.Sleep(150);
			using (var queue = PersistentQueue.WaitFor(SharedStorage, TimeSpan.FromSeconds(30)))
			using (var session = queue.OpenSession())
			{
				session.Enqueue(data);
				session.Flush();
			}
		}

		byte[] ReadQueue()
		{
			Thread.Sleep(150);
			using (var queue = PersistentQueue.WaitFor(SharedStorage, TimeSpan.FromSeconds(30)))
			using (var session = queue.OpenSession())
			{
				var data = session.Dequeue();
				session.Flush();
				return data;
			}
		}

		protected string SharedStorage
		{
			get { return "./MultipleAccess"; }
		}
	}
}