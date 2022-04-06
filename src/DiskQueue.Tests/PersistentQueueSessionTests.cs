using DiskQueue.Implementation;
using NSubstitute;
using NSubstitute.Core;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using DiskQueue.Tests.Helpers;

// ReSharper disable PossibleNullReferenceException

namespace DiskQueue.Tests
{
    [TestFixture]
    public class PersistentQueueSessionTests : PersistentQueueTestsBase
    {
        [Test]
        public void Errors_raised_during_pending_write_will_be_thrown_on_flush()
        {
            var limitedSizeStream = new MemoryStream(new byte[4]);
            var fileStream = new FileStreamWrapper(limitedSizeStream);
            var queueStub = PersistentQueueWithMemoryStream(fileStream);

            var pendingWriteException = Assert.Throws<PendingWriteException>(() =>
            {
                using var session = new PersistentQueueSession(queueStub, fileStream, 1024 * 1024, 1000);
                session.Enqueue(new byte[64 * 1024 * 1024 + 1]);
                session.Flush();
            });

            Assert.That(pendingWriteException.Message, Is.EqualTo("Error during pending writes:\r\n - Memory stream is not expandable."));
        }

        [Test]
        public void Errors_raised_during_flush_write_will_be_thrown_as_is()
        {
            var limitedSizeStream = new MemoryStream(new byte[4]);
            var fileStream = new FileStreamWrapper(limitedSizeStream);
            var queueStub = PersistentQueueWithMemoryStream(fileStream);

            var notSupportedException = Assert.Throws<NotSupportedException>(() =>
            {
                using (var session = new PersistentQueueSession(queueStub, fileStream, 1024 * 1024, 1000))
                {
                    session.Enqueue(new byte[64]);
                    session.Flush();
                }
            });

            Assert.That(notSupportedException.Message, Is.EqualTo(@"Memory stream is not expandable."));
        }

        [Test]
        public void If_data_stream_is_truncated_will_raise_error()
        {
            using (var queue = new PersistentQueue(Path))
            using (var session = queue.OpenSession())
            {
                session.Enqueue(new byte[] { 1, 2, 3, 4 });
                session.Flush();
            }
            using (var fs = new FileStream(System.IO.Path.Combine(Path, "data.0"), FileMode.Open))
            {
                fs.SetLength(2);//corrupt the file
            }

            var invalidOperationException = Assert.Throws<InvalidOperationException>(() =>
            {
                using (var queue = new PersistentQueue(Path))
                {
                    using (var session = queue.OpenSession())
                    {
                        session.Dequeue();
                    }
                }
            });

            Assert.That(invalidOperationException.Message, Is.EqualTo("End of file (no more bytes supplied) reached while trying to read queue item"));
        }
        
        [Test]
        public void If_data_stream_is_truncated_will_NOT_raise_error_if_truncated_entries_are_allowed_in_settings()
        {
            PersistentQueue.DefaultSettings.AllowTruncatedEntries = true;
            PersistentQueue.DefaultSettings.ParanoidFlushing = true;
            
            using (var queue = new PersistentQueue(Path))
            {
                using (var session = queue.OpenSession())
                {
                    session.Enqueue(new byte[] { 1, 2, 3, 4 });
                    session.Flush();
                }
            }
            using (var fs = new FileStream(System.IO.Path.Combine(Path, "data.0"), FileMode.Open))
            {
                fs.SetLength(2);//corrupt the file
            }

            byte[]? bytes;
            using (var queue = new PersistentQueue(Path))
            {
                using (var session = queue.OpenSession())
                {
                    bytes = session.Dequeue();
                }
            }

            PersistentQueue.DefaultSettings.AllowTruncatedEntries = false; // reset to default
            Assert.That(bytes, Is.Null);
        }
        
        [Test]
        public void If_data_stream_is_truncated_the_queue_can_still_be_used()
        {
            PersistentQueue.DefaultSettings.AllowTruncatedEntries = true;
            
            using (var queue = new PersistentQueue(Path))
            {
                using (var session = queue.OpenSession())
                {
                    session.Enqueue(new byte[] { 1, 2, 3, 4 });
                    session.Flush();
                }
            }
            using (var fs = new FileStream(System.IO.Path.Combine(Path, "data.0"), FileMode.Open))
            {
                fs.SetLength(2);//corrupt the file
            }

            using (var queue = new PersistentQueue(Path))
            {
                using (var session = queue.OpenSession())
                {
                    session.Enqueue(new byte[] { 5,6,7,8 });
                    session.Flush();
                }
            }

            byte[]? bytes, corruptBytes;
            using (var queue = new PersistentQueue(Path))
            {
                using (var session = queue.OpenSession())
                {
                    corruptBytes = session.Dequeue();
                    bytes = session.Dequeue();
                }
            }
            
            Console.WriteLine(string.Join(", ", corruptBytes.OrEmpty().Select(b=>b.ToString())));
            Console.WriteLine(string.Join(", ", bytes.OrEmpty().Select(b=>b.ToString())));
            CollectionAssert.AreEqual(new byte[] { 5,6,7,8 }, bytes!);
        }

        private static IPersistentQueueImpl PersistentQueueWithMemoryStream(IFileStream limitedSizeStream)
        {
            var queueStub = Substitute.For<IPersistentQueueImpl>();

            queueStub.WhenForAnyArgs(x => x.AcquireWriter(default!, default!, default!))
                .Do(c => CallActionArgument(c, limitedSizeStream));
            return queueStub!;
        }

        private static void CallActionArgument(CallInfo c, IFileStream ms)
        {
            ((Func<IFileStream, long>)c.Args()[1]!)(ms);
        }
    }
}