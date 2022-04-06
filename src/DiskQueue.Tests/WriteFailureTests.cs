using System;
using System.IO;
using DiskQueue.Implementation;
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
            subject.HardDelete(true);

            using (var session = subject.OpenSession())
            {
                for (int i = 0; i < 100; i++)
                {
                    session.Enqueue(new byte[] { 1, 2, 3, 4 });
                    session.Flush();
                }
            }
            
            using (var session = subject.OpenSession())
            {
                // Switch to a file system that fails on write.
                subject.Internals.SetFileDriver(new WriteFailureDriver());

                for (int i = 0; i < 3; i++)
                {
                    var result = session.Dequeue();
                    Assert.That(result, Is.Not.Null);

                    session.Enqueue(new byte[] { 1, 2, 3, 4 });
                    Assert.Throws<IOException>(() => { session.Flush(); }, "should have thrown an exception when trying to write");
                }
            }
        }
    }

    public class WriteFailureDriver : IFileDriver
    {
        private readonly StandardFileDriver _realDriver;

        public WriteFailureDriver()
        {
            _realDriver = new StandardFileDriver();
        }
        
        public string GetFullPath(string path)=> Path.GetFullPath(path);
        public bool DirectoryExists(string path) => Directory.Exists(path);
        public string PathCombine(string a, string b) => Path.Combine(a,b);

        public Maybe<LockFile> CreateLockFile(string path)
        {
            throw new IOException("Sample CreateLockFile error");
        }

        public void ReleaseLock(LockFile fileLock) { }

        public void PrepareDelete(string path)
        {
            _realDriver.PrepareDelete(path);
        }

        public void Finalise()
        {
            _realDriver.Finalise();
        }

        public void CreateDirectory(string path) { }

        public IFileStream OpenTransactionLog(string path, int bufferLength)
        {
            return _realDriver.OpenTransactionLog(path, bufferLength);
        }

        public IFileStream OpenReadStream(string path)
        {
            return _realDriver.OpenReadStream(path);
        }

        public IFileStream OpenWriteStream(string dataFilePath)
        {
            throw new IOException("Sample OpenWriteStream error");
        }

        public void AtomicRead(string path, Action<IFileStream> action)
        {
            _realDriver.AtomicRead(path, action);
        }

        public void AtomicWrite(string path, Action<IFileStream> action)
        {
            throw new IOException("Sample AtomicWrite error");
        }

        public bool FileExists(string path)
        {
            return _realDriver.FileExists(path);
        }

        public void DeleteRecursive(string path) { }
    }
}