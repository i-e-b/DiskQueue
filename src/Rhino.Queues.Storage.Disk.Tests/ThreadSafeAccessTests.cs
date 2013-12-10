using System;
using System.Threading;
using NUnit.Framework;

namespace DiskQueue.Tests
{
	[TestFixture]
	public class ThreadSafeAccessTests
	{
		[Test]
		public void can_enqueue_and_dequeue_on_separate_threads ()
		{
			int t1s, t2s;
			t1s = t2s = 0;
			const int target = 100;
			var rnd = new Random();


			IPersistentQueue _subject = new PersistentQueue("queue_a");
			var t1 = new Thread(() =>
			{
				for (int i = 0; i < target; i++)
				{
					using (var session = _subject.OpenSession())
					{
						Console.Write("(");
						session.Enqueue(new byte[] { 1, 2, 3, 4 });
						Interlocked.Increment(ref t1s);
						Thread.Sleep(rnd.Next(0, 100));
						session.Flush();
						Console.Write(")");
					}
				}
			});
			var t2 = new Thread(()=> {
				for (int i = 0; i < target; i++)
				{
					using (var session = _subject.OpenSession())
					{
						Console.Write("<");
						session.Dequeue();
						Interlocked.Increment(ref t2s);
						Thread.Sleep(rnd.Next(0, 100));
						session.Flush();
						Console.Write(">");
					}
				}
			});

			t1.Start();
			t2.Start();

			t1.Join();
			t2.Join();
			Assert.That(t1s, Is.EqualTo(target));
			Assert.That(t2s, Is.EqualTo(target));
		}

		[Test]
		public void can_sequence_queues_on_separate_threads ()
		{
			int t1s, t2s;
			t1s = t2s = 0;
			const int target = 100;

			var t1 = new Thread(() =>
			{
				for (int i = 0; i < target; i++)
				{
					using (var subject = PersistentQueue.WaitFor("queue_b", TimeSpan.FromSeconds(10)))
					{
						using (var session = subject.OpenSession())
						{
							Console.Write("(");
							session.Enqueue(new byte[] { 1, 2, 3, 4 });
							Interlocked.Increment(ref t1s);
							session.Flush();
							Console.Write(")");
						}
						Thread.Sleep(0);
					}
				}
			});
			var t2 = new Thread(()=> {
				for (int i = 0; i < target; i++)
				{
					using (var subject = PersistentQueue.WaitFor("queue_b", TimeSpan.FromSeconds(10)))
					{
						using (var session = subject.OpenSession())
						{
							Console.Write("<");
							session.Dequeue();
							Interlocked.Increment(ref t2s);
							session.Flush();
							Console.Write(">");
						}
						Thread.Sleep(0);
					}
				}
			});

			t1.Start();
			t2.Start();

			t1.Join();
			t2.Join();
			Assert.That(t1s, Is.EqualTo(target));
			Assert.That(t2s, Is.EqualTo(target));
		}
	}
}