using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace DiskQueue.Tests
{
	[TestFixture]
	public class TransactionLogTests : PersistentQueueTestsBase
	{
		[Test]
		public void Transaction_log_size_shrink_after_queue_disposed()
		{
			long txSizeWhenOpen;
			var txLogInfo = new FileInfo(Path.Combine(path, "transaction.log"));
			using (var queue = new PersistentQueue(path))
			{
				queue.Internals.ParanoidFlushing = false;
				using (var session = queue.OpenSession())
				{
					for (int j = 0; j < 10; j++)
					{
						session.Enqueue(Guid.NewGuid().ToByteArray());
					}
					session.Flush();
				}

				using (var session = queue.OpenSession())
				{
					for (int j = 0; j < 10; j++)
					{
						session.Dequeue();
					}
					session.Flush();
				}
				txSizeWhenOpen = txLogInfo.Length;
			}
			txLogInfo.Refresh();
			Assert.Less(txLogInfo.Length, txSizeWhenOpen);
		}

		[Test]
		public void Count_of_items_will_remain_fixed_after_dequeqing_without_flushing()
		{
			using (var queue = new PersistentQueue(path))
			{
				queue.Internals.ParanoidFlushing = false;
				using (var session = queue.OpenSession())
				{
					for (int j = 0; j < 10; j++)
					{
						session.Enqueue(Guid.NewGuid().ToByteArray());
					}
					session.Flush();
				}

				using (var session = queue.OpenSession())
				{
					for (int j = 0; j < 10; j++)
					{
						session.Dequeue();
					}
					Assert.IsNull(session.Dequeue());

					//	session.Flush(); explicitly removed
				}
			}
			using (var queue = new PersistentQueue(path))
			{
				Assert.AreEqual(10, queue.EstimatedCountOfItemsInQueue);
			}
		}

		[Test]
		public void Dequeue_items_that_were_not_flushed_will_appear_after_queue_restart()
		{
			using (var queue = new PersistentQueue(path))
			{
				using (var session = queue.OpenSession())
				{
					for (int j = 0; j < 10; j++)
					{
						session.Enqueue(Guid.NewGuid().ToByteArray());
					}
					session.Flush();
				}

				using (var session = queue.OpenSession())
				{
					for (int j = 0; j < 10; j++)
					{
						session.Dequeue();
					}
					Assert.IsNull(session.Dequeue());

					//	session.Flush(); explicitly removed
				}
			}
			using (var queue = new PersistentQueue(path))
			{
				using (var session = queue.OpenSession())
				{
					for (int j = 0; j < 10; j++)
					{
						session.Dequeue();
					}
					Assert.IsNull(session.Dequeue());
					session.Flush();
				}
			}
		}

		[Test]
		public void If_tx_log_grows_too_large_it_will_be_trimmed_while_queue_is_in_operation()
		{
			var txLogInfo = new FileInfo(Path.Combine(path, "transaction.log"));

			using (var queue = new PersistentQueue(path)
			{
				SuggestedMaxTransactionLogSize = 32 // single entry
			})
			{
				queue.Internals.ParanoidFlushing = false;

				using (var session = queue.OpenSession())
				{
					for (int j = 0; j < 20; j++)
					{
						session.Enqueue(Guid.NewGuid().ToByteArray());
					}
					session.Flush();
				}
				// there is no way optimize here, so we should get expected size, even though it is bigger than
				// what we suggested as the max
				txLogInfo.Refresh();
				long txSizeWhenOpen = txLogInfo.Length;

				using (var session = queue.OpenSession())
				{
					for (int j = 0; j < 20; j++)
					{
						session.Dequeue();
					}
					Assert.IsNull(session.Dequeue());

					session.Flush();
				}
				txLogInfo.Refresh();
				Assert.Less(txLogInfo.Length, txSizeWhenOpen);
			}
		}

		[Test]
		public void Truncated_transaction_is_ignored()
		{
			var txLogInfo = new FileInfo(Path.Combine(path, "transaction.log"));

			using (var queue = new PersistentQueue(path)
			{
				// avoid auto tx log trimming
				TrimTransactionLogOnDispose = false
			})
			{
				queue.Internals.ParanoidFlushing = false;

				using (var session = queue.OpenSession())
				{
					for (int j = 0; j < 20; j++)
					{
						session.Enqueue(BitConverter.GetBytes(j));
						session.Flush();
					}
				}
			}

			using (var txLog = txLogInfo.Open(FileMode.Open))
			{
				txLog.SetLength(txLog.Length - 5);// corrupt last transaction
				txLog.Flush();
			}

			using (var queue = new PersistentQueue(path))
			{
				using (var session = queue.OpenSession())
				{
					for (int j = 0; j < 19; j++)
					{
						Assert.AreEqual(j, BitConverter.ToInt32(session.Dequeue(), 0));
					}
					Assert.IsNull(session.Dequeue());// the last transaction was corrupted
					session.Flush();
				}
			}
		}

		[Test]
		public void Can_handle_truncated_start_transaction_seperator()
		{
			var txLogInfo = new FileInfo(Path.Combine(path, "transaction.log"));

			using (var queue = new PersistentQueue(path)
			{
				// avoid auto tx log trimming
				TrimTransactionLogOnDispose = false
			})
			{
				using (var session = queue.OpenSession())
				{
					for (int j = 0; j < 20; j++)
					{
						session.Enqueue(BitConverter.GetBytes(j));
						session.Flush();
					}
				}
			}

			using (var txLog = txLogInfo.Open(FileMode.Open))
			{
				txLog.SetLength(5);// corrupt last transaction
				txLog.Flush();
			}

			using (var queue = new PersistentQueue(path))
			{
				using (var session = queue.OpenSession())
				{
					Assert.IsNull(session.Dequeue());// the last transaction was corrupted
					session.Flush();
				}
			}
		}

		[Test]
		public void Will_remove_truncated_transaction()
		{
			var txLogInfo = new FileInfo(Path.Combine(path, "transaction.log"));

			using (var queue = new PersistentQueue(path))
			{
				using (var session = queue.OpenSession())
				{
					for (int j = 0; j < 20; j++)
					{
						session.Enqueue(BitConverter.GetBytes(j));
						session.Flush();
					}
				}
			}

			using (var txLog = txLogInfo.Open(FileMode.Open))
			{
				txLog.SetLength(5);// corrupt all transactions
				txLog.Flush();
			}

			new PersistentQueue(path).Dispose();

			txLogInfo.Refresh();

			Assert.AreEqual(36, txLogInfo.Length);//empty transaction size
		}

		[Test]
		public void Truncated_transaction_is_ignored_and_can_continue_to_add_items_to_queue()
		{
			var txLogInfo = new FileInfo(Path.Combine(path, "transaction.log"));

			using (var queue = new PersistentQueue(path)
			{
				// avoid auto tx log trimming
				TrimTransactionLogOnDispose = false
			})
			{
				queue.Internals.ParanoidFlushing = false;
				using (var session = queue.OpenSession())
				{
					for (int j = 0; j < 20; j++)
					{
						session.Enqueue(BitConverter.GetBytes(j));
						session.Flush();
					}
				}
			}

			using (var txLog = txLogInfo.Open(FileMode.Open))
			{
				txLog.SetLength(txLog.Length - 5);// corrupt last transaction
				txLog.Flush();
			}

			using (var queue = new PersistentQueue(path)
			{
				// avoid auto tx log trimming
				TrimTransactionLogOnDispose = false
			})
			{
				using (var session = queue.OpenSession())
				{
					for (int j = 20; j < 40; j++)
					{
						session.Enqueue(BitConverter.GetBytes(j));
					}
					session.Flush();
				}
			}

			var data = new List<int>();
			using (var queue = new PersistentQueue(path))
			{
				using (var session = queue.OpenSession())
				{
					var dequeue = session.Dequeue();
					while (dequeue != null)
					{
						data.Add(BitConverter.ToInt32(dequeue, 0));
						dequeue = session.Dequeue();
					}
					session.Flush();
				}
			}
			var expected = 0;
			foreach (var i in data)
			{
				if (expected == 19)
					continue;
				Assert.AreEqual(expected, data[i]);
				expected++;
			}
		}
	}
}