using System;
using System.Threading;
using NUnit.Framework;

namespace DiskQueue.Tests
{
    [TestFixture]
    public class WriteFailureTests
    {
        [Test]
        public void enqueue_fails_if_disk_is_full_but_dequeue_still_works ()
        {
            IPersistentQueue subject = new PersistentQueue("queue_a");

            using (var session = subject.OpenSession())
            {
                for (int i = 0; i < 100; i++)
                {
                    session.Enqueue(new byte[] { 1, 2, 3, 4 });
                    session.Flush();
                }
            }
           
            // Switch to a file system that fails on write.
            subject.Internals.SetFileDriver(new WriteFailureDriver()); 
            
        }
    }

    public class WriteFailureDriver : IFileDriver
    {
    }
}