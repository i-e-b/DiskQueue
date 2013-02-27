namespace Rhino.Queues.Storage.Disk.Tests
{
	using System;
	using MbUnit.Framework;
	using System.Linq;

	[TestFixture]
	public class PendingWriteExceptionTests
	{
		[Test]
		public void Can_get_all_information_from_to_string()
		{
			try
			{
				throw new ArgumentException("foo");
			}
			catch (Exception e)
			{
				var s = new PendingWriteException(new []{e}).ToString();
				Assert.IsTrue(
					s.Contains(e.ToString())
					);
			}
		}

		[Test]
		public void Can_get_exception_detail_information_from_pending_write_exception()
		{
			try
			{
				throw new ArgumentException("foo");
			}
			catch (Exception e)
			{
				var s = new PendingWriteException(new [] { e });
				Assert.IsTrue(
					s.PendingWritesExceptions.Contains(e)
					);
			}
		}
	}
}