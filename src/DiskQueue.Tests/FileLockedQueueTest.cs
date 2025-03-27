using DiskQueue.Implementation;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;

namespace DiskQueue.Tests
{
    [TestFixture]
    public class FileLockedQueueTest
    {
        private Process? _otherProcess;
        private Process? _currentProcess;
        private int _currentThread;

        [OneTimeSetUp]
        public void OneTimeSetUp() 
        {
            _currentProcess = Process.GetCurrentProcess();
            _currentThread = Environment.CurrentManagedThreadId;

            if (File.Exists("TestDummyProcess.exe")) _otherProcess = Process.Start("TestDummyProcess.exe");
            else if (File.Exists("TestDummyProcess")) _otherProcess = Process.Start("TestDummyProcess");
            else Assert.Inconclusive("Can't start test process");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _otherProcess?.Kill();
        }

        [Test]
        public void FileIsLockedAfterPowerFailure_QueueObtainsLock()
        {
            //ARRANGE
            var queueName = GetQueueName();
            WriteLockFile(queueName, _otherProcess!.Id, _currentThread, _otherProcess.StartTime.AddSeconds(1));

            //ACT
            using var queue = new PersistentQueue<string>(queueName);
        }

        [Test]
        public void FileIsLockedByCurrentProcessButWrongThread_ThrowsException()
        {
            //ARRANGE
            var queueName = GetQueueName();
            WriteLockFile(queueName, _currentProcess!.Id, 555, _currentProcess.StartTime);

            try
            {
                //ACT
                using var queue = new PersistentQueue<string>(queueName);
            }
            catch(InvalidOperationException ex)
            {
                //ASSERT
                Assert.That(ex.InnerException?.Message, Is.EqualTo("This queue is locked by another thread in this process. Thread id = 555"));
                Assert.Pass();
            }
            Assert.Fail();
        }

        [Test]
        public void FileIsLockedByOtherRunningProcess_ThrowsException()
        {
            //ARRANGE
            var queueName = GetQueueName();
            WriteLockFile(queueName, _otherProcess!.Id, 555, _otherProcess.StartTime);

            try
            {
                //ACT
                using var queue = new PersistentQueue<string>(queueName);
            }
            catch (InvalidOperationException ex)
            {
                //ASSERT
                Assert.That(ex.InnerException?.Message, Is.EqualTo($"This queue is locked by another running process. Process id = {_otherProcess.Id}"));
                Assert.Pass();
            }
            Assert.Fail();
        }

        private static string GetQueueName()
        {
            var valueToTest = "a string";
            var hash = valueToTest.GetHashCode().ToString("X8");
            return $"./LockQueueTests_{hash}";
        }

        private static void WriteLockFile(string queueName, int processId, int threadId, DateTime startTime)
        {
            var lockData = new LockFileData
            {
                ProcessId = processId,
                ThreadId = threadId,
                ProcessStart = ((DateTimeOffset)startTime).ToUnixTimeMilliseconds(),
            };
            Directory.CreateDirectory(queueName);
            File.WriteAllBytes($@"{queueName}\lock", MarshallHelper.Serialize(lockData));
        }
    }
}
