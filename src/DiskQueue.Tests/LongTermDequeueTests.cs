using System;
using System.Linq;
using NUnit.Framework;

namespace DiskQueue.Tests
{
	[TestFixture]
	public class LongTermDequeueTests
	{
		IPersistentQueue _q;

		[SetUp]
		public void setup()
		{
			_q = PersistentQueue.WaitFor("./queue", TimeSpan.FromSeconds(10));
		}

		[TearDown]
		public void teardown()
		{
			_q.Dispose();
		}

		[Test]
		public void can_enqueue_during_a_long_dequeue ()
		{
			var s1 = _q.OpenSession();

			using (var s2 = _q.OpenSession())
			{
				s2.Enqueue(new byte[]{1,2,3,4});
				s2.Flush();
			}

			var x = s1.Dequeue();
			s1.Flush();
			s1.Dispose();

			Assert.That(x.SequenceEqual(new byte[]{1,2,3,4}));
		}

	}
}