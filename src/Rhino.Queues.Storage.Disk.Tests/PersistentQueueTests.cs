using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace DiskQueue.Tests
{
	[TestFixture]
	public class PersistentQueueTests : PersistentQueueTestsBase
	{
		[Test]
		[ExpectedException(typeof(InvalidOperationException),
			ExpectedMessage = "Another instance of the queue is already in action, or directory does not exists")]
		public void Only_single_instance_of_queue_can_exists_at_any_one_time()
		{
			using (new PersistentQueue(path))
			{
				new PersistentQueue(path);
			}
		}

		[Test]
		public void If_a_non_running_process_has_a_lock_then_can_start_an_instance ()
		{
			Directory.CreateDirectory(path);
			var lockFilePath = Path.Combine(path, "lock");
			File.WriteAllText(lockFilePath, "78924759045");
			
			using (new PersistentQueue(path))
			{
				Assert.Pass();
			}
		}

		[Test]
		public void Can_create_new_queue()
		{
			new PersistentQueue(path).Dispose();
		}

		[Test]
		[ExpectedException(typeof(InvalidOperationException), ExpectedMessage ="Unexpected data in transaction log. Expected to get transaction separator but got unknown data. Tx #1")]
		public void Corrupt_index_file_should_throw()
		{
			var buffer = new List<byte>();
			buffer.AddRange(Guid.NewGuid().ToByteArray());
			buffer.AddRange(Guid.NewGuid().ToByteArray());
			buffer.AddRange(Guid.NewGuid().ToByteArray());

			Directory.CreateDirectory(path);
			File.WriteAllBytes(Path.Combine(path, "transaction.log"), buffer.ToArray());

			new PersistentQueue(path);
		}

		[Test]
		public void Dequeing_from_empty_queue_will_return_null()
		{
			using (var queue = new PersistentQueue(path))
			using (var session = queue.OpenSession())
			{
				Assert.IsNull(session.Dequeue());
			}
		}

		[Test]
		public void Can_enqueue_data_in_queue()
		{
			using (var queue = new PersistentQueue(path))
			using (var session = queue.OpenSession())
			{
				session.Enqueue(new byte[] { 1, 2, 3, 4 });
				session.Flush();
			}
		}

		[Test]
		public void Can_dequeue_data_from_queue()
		{
			using (var queue = new PersistentQueue(path))
			using (var session = queue.OpenSession())
			{
				session.Enqueue(new byte[] { 1, 2, 3, 4 });
				session.Flush();
				CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, session.Dequeue());
			}
		}

		[Test]
		public void Can_enqueue_and_dequeue_data_after_restarting_queue()
		{
			using (var queue = new PersistentQueue(path))
			using (var session = queue.OpenSession())
			{
				session.Enqueue(new byte[] { 1, 2, 3, 4 });
				session.Flush();
			}

			using (var queue = new PersistentQueue(path))
			using (var session = queue.OpenSession())
			{
				CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, session.Dequeue());
				session.Flush();
			}
		}

		[Test]
		public void After_dequeue_from_queue_item_no_longer_on_queue()
		{
			using (var queue = new PersistentQueue(path))
			using (var session = queue.OpenSession())
			{
				session.Enqueue(new byte[] { 1, 2, 3, 4 });
				session.Flush();
			}

			using (var queue = new PersistentQueue(path))
			using (var session = queue.OpenSession())
			{
				CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, session.Dequeue());
				Assert.IsNull(session.Dequeue());
				session.Flush();
			}
		}

		[Test]
		public void After_dequeue_from_queue_item_no_longer_on_queue_with_queues_restarts()
		{
			using (var queue = new PersistentQueue(path))
			using (var session = queue.OpenSession())
			{
				session.Enqueue(new byte[] { 1, 2, 3, 4 });
				session.Flush();
			}

			using (var queue = new PersistentQueue(path))
			using (var session = queue.OpenSession())
			{
				CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, session.Dequeue());
				session.Flush();
			}

			using (var queue = new PersistentQueue(path))
			using (var session = queue.OpenSession())
			{
				Assert.IsNull(session.Dequeue());
				session.Flush();
			}
		}

		[Test]
		public void Not_flushing_the_session_will_revert_dequequed_items()
		{
			using (var queue = new PersistentQueue(path))
			using (var session = queue.OpenSession())
			{
				session.Enqueue(new byte[] { 1, 2, 3, 4 });
				session.Flush();
			}

			using (var queue = new PersistentQueue(path))
			using (var session = queue.OpenSession())
			{
				CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, session.Dequeue());
				//Explicitly ommitted: session.Flush();
			}

			using (var queue = new PersistentQueue(path))
			using (var session = queue.OpenSession())
			{
				CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, session.Dequeue());
				session.Flush();
			}
		}

		[Test]
		public void Not_flushing_the_session_will_revert_dequequed_items_two_sessions_same_queue()
		{
			using (var queue = new PersistentQueue(path))
			using (var session = queue.OpenSession())
			{
				session.Enqueue(new byte[] { 1, 2, 3, 4 });
				session.Flush();
			}

			using (var queue = new PersistentQueue(path))
			using (var session2 = queue.OpenSession())
			{
				using (var session1 = queue.OpenSession())
				{
					CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, session1.Dequeue());
					//Explicitly ommitted: session.Flush();
				}
				CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, session2.Dequeue());
				session2.Flush();
			}
		}

		[Test]
		public void Two_sessions_off_the_same_queue_cannot_get_same_item()
		{
			using (var queue = new PersistentQueue(path))
			using (var session = queue.OpenSession())
			{
				session.Enqueue(new byte[] { 1, 2, 3, 4 });
				session.Flush();
			}

			using (var queue = new PersistentQueue(path))
			using (var session2 = queue.OpenSession())
			using (var session1 = queue.OpenSession())
			{
				CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, session1.Dequeue());
				Assert.IsNull(session2.Dequeue());
			}
		}
	}
}