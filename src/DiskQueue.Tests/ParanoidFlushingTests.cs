using NUnit.Framework;

namespace DiskQueue.Tests
{
	[TestFixture]
	public class ParanoidFlushingTests
	{
		readonly byte[] _one = new byte[] { 1, 2, 3, 4 };
		readonly byte[] _two = new byte[] { 5, 6, 7, 8 };

		[Test]
		public void Paranoid_flushing_still_respects_session_rollback ()
		{
			using (var queue = new PersistentQueue("./queue"))
			{
				queue.Internals.ParanoidFlushing = true;

				// Flush only `_one`
				using (var s1 = queue.OpenSession())
				{
					s1.Enqueue(_one);
					s1.Flush();
					s1.Enqueue(_two);
				}

				// Read without flushing
				using (var s2 = queue.OpenSession())
				{
					Assert.That(s2.Dequeue(), Is.EquivalentTo(_one), "Unexpected item at head of queue");
					Assert.That(s2.Dequeue(), Is.Null, "Too many items on queue");
				}

				// Read again WITH flushing
				using (var s3 = queue.OpenSession())
				{
					Assert.That(s3.Dequeue(), Is.EquivalentTo(_one), "Queue was unexpectedly empty?");
					Assert.That(s3.Dequeue(), Is.Null, "Too many items on queue");
					s3.Flush();
				}

				// Read empty queue to be sure
				using (var s4 = queue.OpenSession())
				{
					Assert.That(s4.Dequeue(), Is.Null, "Queue was not empty after flush");
					s4.Flush();
				}
			}
		}
	}
}