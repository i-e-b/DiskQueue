using NUnit.Framework;
// ReSharper disable PossibleNullReferenceException

namespace DiskQueue.Tests
{
	[TestFixture]
	public class ParanoidFlushingTests
	{
		private readonly byte[] _one = { 1, 2, 3, 4 };
		private readonly byte[] _two = { 5, 6, 7, 8 };

		[Test]
		public void Paranoid_flushing_still_respects_session_rollback ()
		{
			using (var queue = new PersistentQueue("./queue"))
			{
				// Clean up leftover data from previous failed test runs
                queue.HardDelete(true);

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