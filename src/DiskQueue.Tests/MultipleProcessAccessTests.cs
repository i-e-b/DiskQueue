using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

// ReSharper disable PossibleNullReferenceException

namespace DiskQueue.Tests
{
    [TestFixture]
    public class MultipleProcessAccessTests : PersistentQueueTestsBase
    {
        protected override string Path => "./MultipleProcessAccessTests";

        [Test,
         Description("Multiple PersistentQueue instances are " +
                     "pretty much the same as multiple processes to " +
                     "the DiskQueue library")]
        public void Can_access_from_multiple_queues_if_used_carefully()
        {
            var received = new List<byte[]>();
            int numberOfItems = 10;

            var t1 = new Thread(() =>
            {
                for (int i = 0; i < numberOfItems; i++)
                {
                    AddToQueue(new byte[] { 1, 2, 3 });
                }
            });
            var t2 = new Thread(() =>
            {
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
                Assert.Fail("Did not receive all data in time");
            }

            Assert.That(received.Count, Is.EqualTo(numberOfItems), "received items");
        }

        [Test]
        public void Can_access_from_multiple_queues_if_used_carefully_with_generic_container_and_serialisation()
        {
            var received = new List<string>();
            int numberOfItems = 10;

            var t1 = new Thread(() =>
            {
                for (int i = 0; i < numberOfItems; i++)
                {
                    AddToQueueString("Hello");
                }
            });
            var t2 = new Thread(() =>
            {
                while (received.Count < numberOfItems)
                {
                    var data = ReadQueueString();
                    if (data != null) received.Add(data);
                }
            });

            t1.Start();
            t2.Start();

            t1.Join();

            var ok = t2.Join(TimeSpan.FromSeconds(25));

            if (!ok)
            {
                Assert.Fail("Did not receive all data in time");
            }

            Assert.That(received.Count, Is.EqualTo(numberOfItems), "received items");
        }

        private void AddToQueueString(string data)
        {
            Thread.Sleep(152);
            using var queue = PersistentQueue.WaitFor<string>(Path, TimeSpan.FromSeconds(30));
            using var session = queue.OpenSession();

            session.Enqueue(data);
            session.Flush();
        }

        private string? ReadQueueString()
        {
            Thread.Sleep(121);
            using var queue = PersistentQueue.WaitFor<string>(Path, TimeSpan.FromSeconds(30));
            using var session = queue.OpenSession();
            var data = session.Dequeue();
            session.Flush();
            return data;
        }

        private void AddToQueue(byte[] data)
        {
            Thread.Sleep(152);
            using (var queue = PersistentQueue.WaitFor(Path, TimeSpan.FromSeconds(30)))
            using (var session = queue.OpenSession())
            {
                session.Enqueue(data);
                session.Flush();
            }
        }

        private byte[]? ReadQueue()
        {
            Thread.Sleep(121);
            using (var queue = PersistentQueue.WaitFor(Path, TimeSpan.FromSeconds(30)))
            using (var session = queue.OpenSession())
            {
                var data = session.Dequeue();
                session.Flush();
                return data;
            }
        }
    }
}