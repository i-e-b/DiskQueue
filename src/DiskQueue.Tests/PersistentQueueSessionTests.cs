using DiskQueue.Implementation;
using NSubstitute;
using NSubstitute.Core;
using NUnit.Framework;
using System;
using System.IO;

namespace DiskQueue.Tests
{
    [TestFixture]
    public class PersistentQueueSessionTests : PersistentQueueTestsBase
    {
        [Test]
        //	[ExpectedException(typeof(PendingWriteException),ExpectedMessage = @"Error during pending writes:
        //- Memory stream is not expandable.")]
        public void Errors_raised_during_pending_write_will_be_thrown_on_flush()
        {
            var limitedSizeStream = new MemoryStream(new byte[4]);
            var queueStub = PersistentQueueWithMemoryStream(limitedSizeStream);

            var pendingWriteException = Assert.Throws<PendingWriteException>(() =>
            {
                using (var session = new PersistentQueueSession(queueStub, limitedSizeStream, 1024 * 1024))
                {
                    session.Enqueue(new byte[64 * 1024 * 1024 + 1]);
                    session.Flush();
                }
            });

            Assert.That(pendingWriteException.Message, Is.EqualTo(@"Error during pending writes:
 - Memory stream is not expandable."));
        }

        [Test]
        //[ExpectedException(typeof(NotSupportedException),ExpectedMessage = @"Memory stream is not expandable.")]
        public void Errors_raised_during_flush_write_will_be_thrown_as_is()
        {
            var limitedSizeStream = new MemoryStream(new byte[4]);
            var queueStub = PersistentQueueWithMemoryStream(limitedSizeStream);

            var notSupportedException = Assert.Throws<NotSupportedException>(() =>
            {
                using (var session = new PersistentQueueSession(queueStub, limitedSizeStream, 1024 * 1024))
                {
                    session.Enqueue(new byte[64]);
                    session.Flush();
                }
            });

            Assert.That(notSupportedException.Message, Is.EqualTo(@"Memory stream is not expandable."));
        }

        [Test]
        //[ExpectedException(typeof(InvalidOperationException),ExpectedMessage =  "End of file reached while trying to read queue item")]
        public void If_data_stream_is_truncated_will_raise_error()
        {
            using (var queue = new PersistentQueue(path))
            using (var session = queue.OpenSession())
            {
                session.Enqueue(new byte[] { 1, 2, 3, 4 });
                session.Flush();
            }
            using (var fs = new FileStream(Path.Combine(path, "data.0"), FileMode.Open))
            {
                fs.SetLength(2);//corrupt the file
            }

            var invalidOperationException = Assert.Throws<InvalidOperationException>(() =>
            {
                using (var queue = new PersistentQueue(path))
                {
                    using (var session = queue.OpenSession())
                    {
                        session.Dequeue();
                    }
                }
            });

            Assert.That(invalidOperationException.Message, Is.EqualTo("End of file reached while trying to read queue item"));
        }

        static IPersistentQueueImpl PersistentQueueWithMemoryStream(MemoryStream limitedSizeStream)
        {
            var queueStub = Substitute.For<IPersistentQueueImpl>();

            queueStub.WhenForAnyArgs(x => x.AcquireWriter(null, null, null))
                .Do(c => CallActionArgument(c, limitedSizeStream));
            return queueStub;
        }

        static void CallActionArgument(CallInfo c, MemoryStream ms)
        {
            ((Func<Stream, long>)c.Args()[1])(ms);
        }
    }
}