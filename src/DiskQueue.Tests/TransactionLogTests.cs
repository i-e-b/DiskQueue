using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using DiskQueue.Implementation;
// ReSharper disable PossibleNullReferenceException
// ReSharper disable AssignNullToNotNullAttribute

namespace DiskQueue.Tests
{
	[TestFixture, SingleThreaded]
	public class TransactionLogTests : PersistentQueueTestsBase
	{
		[Test]
		public void Transaction_log_size_shrink_after_queue_disposed()
		{
			long txSizeWhenOpen;
			var txLogInfo = new FileInfo(System.IO.Path.Combine(Path, "transaction.log"));
			using (var queue = new PersistentQueue(Path))
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
		public void Count_of_items_will_remain_fixed_after_dequeueing_without_flushing()
		{
			using (var queue = new PersistentQueue(Path))
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
			using (var queue = new PersistentQueue(Path))
			{
				Assert.AreEqual(10, queue.EstimatedCountOfItemsInQueue);
			}
		}

		[Test]
		public void Dequeue_items_that_were_not_flushed_will_appear_after_queue_restart()
		{
			using (var queue = new PersistentQueue(Path))
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
			using (var queue = new PersistentQueue(Path))
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
			var txLogInfo = new FileInfo(System.IO.Path.Combine(Path, "transaction.log"));

			using (var queue = new PersistentQueue(Path)
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
		public void Truncated_transaction_is_ignored_with_default_settings()
		{
			var txLogInfo = new FileInfo(System.IO.Path.Combine(Path, "transaction.log"));

			using (var queue = new PersistentQueue(Path)
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

			using (var queue = new PersistentQueue(Path))
			{
				using (var session = queue.OpenSession())
				{
					for (int j = 0; j < 19; j++)
					{
						var bytes = session.Dequeue() ?? throw new Exception("read failed");
						Assert.AreEqual(j, BitConverter.ToInt32(bytes, 0));
					}
					Assert.IsNull(session.Dequeue());// the last transaction was corrupted
					session.Flush();
				}
			}
		}

		[Test]
		public void Can_handle_truncated_start_transaction_separator()
		{
			var txLogInfo = new FileInfo(System.IO.Path.Combine(Path, "transaction.log"));

			using (var queue = new PersistentQueue(Path)
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
				txLog.SetLength(5); // truncate log to halfway through start marker
				txLog.Flush();
			}

			using (var queue = new PersistentQueue(Path))
			{
				using (var session = queue.OpenSession())
				{
					Assert.IsNull(session.Dequeue());// the last transaction was corrupted
					session.Flush();
				}
			}
		}

	    [Test]
	    public void Can_handle_truncated_data()
	    {
	        var txLogInfo = new FileInfo(System.IO.Path.Combine(Path, "transaction.log"));

	        using (var queue = new PersistentQueue(Path)
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
	            txLog.SetLength(100); // truncate log to halfway through log entry
	            txLog.Flush();
	        }

	        using (var queue = new PersistentQueue(Path))
	        {
	            using (var session = queue.OpenSession())
	            {
	                Assert.IsNull(session.Dequeue());// the last transaction was corrupted
	                session.Flush();
	            }
	        }
	    }

	    [Test]
	    public void Can_handle_truncated_end_transaction_separator()
	    {
	        var txLogInfo = new FileInfo(System.IO.Path.Combine(Path, "transaction.log"));

	        using (var queue = new PersistentQueue(Path)
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
	            txLog.SetLength(368); // truncate end transaction marker
	            txLog.Flush();
	        }

	        using (var queue = new PersistentQueue(Path))
	        {
	            using (var session = queue.OpenSession())
	            {
	                Assert.IsNull(session.Dequeue());// the last transaction was corrupted
	                session.Flush();
	            }
	        }
	    }



	    [Test]
	    public void Can_handle_transaction_with_only_zero_length_entries()
	    {
	        using (var queue = new PersistentQueue(Path)
	        {
	            // avoid auto tx log trimming
	            TrimTransactionLogOnDispose = false
	        })
	        {
	            using (var session = queue.OpenSession())
	            {
	                for (int j = 0; j < 20; j++)
	                {
	                    session.Enqueue(new byte[0]);
	                    session.Flush();
	                }
	            }
	        }

	        using (var queue = new PersistentQueue(Path))
	        {
	            using (var session = queue.OpenSession())
	            {
	                for (int j = 0; j < 20; j++)
	                {
	                    Assert.IsEmpty(session.Dequeue());
	                }
	                Assert.IsNull(session.Dequeue());
	                session.Flush();
	            }
	        }
	    }
        
	    [Test]
	    public void Can_handle_end_separator_used_as_data()
	    {
	        using (var queue = new PersistentQueue(Path)
	        {
	            // avoid auto tx log trimming
	            TrimTransactionLogOnDispose = false
	        })
            {
                using (var session = queue.OpenSession())
                {
                    for (int j = 0; j < 20; j++)
                    {
                        session.Enqueue(Constants.EndTransactionSeparator); // ???
                        session.Flush();
                    }
                    session.Flush();
                }
            }

            using (var queue = new PersistentQueue(Path))
	        {
	            using (var session = queue.OpenSession())
	            {
	                Assert.AreEqual(Constants.EndTransactionSeparator, session.Dequeue());
	                session.Flush();
	            }
	        }
	    }
        
	    [Test]
	    public void Can_handle_start_separator_used_as_data()
	    {
	        using (var queue = new PersistentQueue(Path)
	        {
	            // avoid auto tx log trimming
	            TrimTransactionLogOnDispose = false
	        })
	        {
	            using (var session = queue.OpenSession())
	            {
	                for (int j = 0; j < 20; j++)
	                {
	                    session.Enqueue(Constants.StartTransactionSeparator); // ???
	                    session.Flush();
	                }
	                session.Flush();
	            }
	        }

	        using (var queue = new PersistentQueue(Path))
	        {
	            using (var session = queue.OpenSession())
	            {
	                Assert.AreEqual(Constants.StartTransactionSeparator, session.Dequeue());
	                session.Flush();
	            }
	        }
	    }
        
	    [Test]
	    public void Can_handle_zero_length_entries_at_start()
	    {
	        using (var queue = new PersistentQueue(Path)
	        {
	            // avoid auto tx log trimming
	            TrimTransactionLogOnDispose = false
	        })
	        {
	            using (var session = queue.OpenSession())
	            {
	                session.Enqueue(new byte[0]);
	                session.Flush();
	                for (int j = 0; j < 19; j++)
	                {
	                    session.Enqueue(new byte[] {1});
	                    session.Flush();
	                }
	            }
	        }

	        using (var queue = new PersistentQueue(Path))
	        {
	            using (var session = queue.OpenSession())
	            {
	                for (int j = 0; j < 20; j++)
	                {
	                    Assert.IsNotNull(session.Dequeue());
	                    session.Flush();
	                }
	            }
	        }
	    }

        
	    [Test]
	    public void Can_handle_zero_length_entries_at_end()
	    {
	        using (var queue = new PersistentQueue(Path)
	        {
	            // avoid auto tx log trimming
	            TrimTransactionLogOnDispose = false
	        })
	        {
	            using (var session = queue.OpenSession())
	            {
	                for (int j = 0; j < 19; j++)
	                {
	                    session.Enqueue(new byte[] {1});
	                    session.Flush();
	                }
	                session.Enqueue(new byte[0]);
	                session.Flush();
	            }
	        }

	        using (var queue = new PersistentQueue(Path))
	        {
	            using (var session = queue.OpenSession())
	            {
	                for (int j = 0; j < 20; j++)
	                {
	                    Assert.IsNotNull(session.Dequeue());
	                    session.Flush();
	                }
	            }
	        }
	    }

	    [Test]
	    public void Can_restore_data_when_a_transaction_set_is_partially_truncated()
	    {
		    PersistentQueue.DefaultSettings.AllowTruncatedEntries = false;
	        var txLogInfo = new FileInfo(System.IO.Path.Combine(Path, "transaction.log"));
	        using (var queue = new PersistentQueue(Path)
	        {
	            // avoid auto tx log trimming
	            TrimTransactionLogOnDispose = false
	        })
	        {
	            using (var session = queue.OpenSession())
	            {
	                for (int j = 0; j < 5; j++)
	                {
	                    session.Enqueue(new[]{(byte)(j+1)});
	                }
                    session.Flush();
                }
	        }
			
	        using (var txLog = txLogInfo.Open(FileMode.Open))
	        {
                var buf = new byte[(int)txLog.Length];
	            var actual = txLog.Read(buf, 0, (int)txLog.Length);
	            Assert.AreEqual(txLog.Length, actual);
	            txLog.Write(buf, 0, buf.Length);        // a 'good' extra session
	            txLog.Write(buf, 0, buf.Length / 2);    // a 'bad' extra session
	            txLog.Flush();
	        }

	        using (var queue = new PersistentQueue(Path))
	        {
	            using (var session = queue.OpenSession())
	            {
	                for (int j = 0; j < 5; j++)
	                {
	                    Assert.That(session.Dequeue(), Is.EquivalentTo(new[]{(byte)(j+1)}));
	                    session.Flush();
	                }
	                for (int j = 0; j < 5; j++)
	                {
		                Assert.That(session.Dequeue(), Is.EquivalentTo(new[]{(byte)(j+1)}));
		                session.Flush();
	                }
	                
	                Assert.IsNull(session.Dequeue());
	                session.Flush();
	            }
	        }
	    }

        [Test]
	    public void Can_restore_data_when_a_transaction_set_is_partially_overwritten_when_throwOnConflict_is_false()
	    {
	        var txLogInfo = new FileInfo(System.IO.Path.Combine(Path, "transaction.log"));
	        using (var queue = new PersistentQueue(Path)
	        {
	            // avoid auto tx log trimming
	            TrimTransactionLogOnDispose = false
	        })
	        {
	            using (var session = queue.OpenSession())
	            {
	                for (int j = 0; j < 5; j++)
	                {
	                    session.Enqueue(Array.Empty<byte>());
	                }
	                session.Flush();
	            }
	        }
            
	        using (var txLog = txLogInfo.Open(FileMode.Open))
	        {
	            var buf = new byte[(int)txLog.Length];
	            var actual = txLog.Read(buf, 0, (int)txLog.Length);
	            Assert.AreEqual(txLog.Length, actual);
	            txLog.Write(buf, 0, buf.Length - 16); // new session, but with missing end marker
	            txLog.Write(Constants.StartTransactionSeparator, 0, 16);
	            txLog.Flush();
	        }

	        using (var queue = new PersistentQueue(Path, Constants._32Megabytes, throwOnConflict: false))
	        {
	            using (var session = queue.OpenSession())
	            {
	                for (int j = 0; j < 5; j++) // first 5 should be OK
	                {
	                    Assert.IsNotNull(session.Dequeue());
	                }
	                Assert.IsNull(session.Dequeue()); // duplicated 5 should be silently lost.
	                session.Flush();
	            }
	        }
	    }

	    [Test]
		public void Will_remove_truncated_transaction()
		{
			var txLogInfo = new FileInfo(System.IO.Path.Combine(Path, "transaction.log"));

			using (var queue = new PersistentQueue(Path))
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

			new PersistentQueue(Path).Dispose();

			txLogInfo.Refresh();

			Assert.AreEqual(36, txLogInfo.Length);//empty transaction size
		}

		[Test]
		public void Truncated_transaction_is_ignored_and_can_continue_to_add_items_to_queue()
		{
			var txLogInfo = new FileInfo(System.IO.Path.Combine(Path, "transaction.log"));

			using (var queue = new PersistentQueue(Path)
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

			using (var queue = new PersistentQueue(Path)
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
			using (var queue = new PersistentQueue(Path))
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