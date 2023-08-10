using DiskQueue.Implementation;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiskQueue.Tests
{
    [TestFixture]
    public class FileLockedQueueTest
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private Process _otherProcess;
        private Process _currentProcess;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private int _currentThread;

        [OneTimeSetUp]
        public void OneTimeSetUp() 
        {
            _currentProcess = Process.GetCurrentProcess();
            _currentThread = Thread.CurrentThread.ManagedThreadId;
            _otherProcess = Process.GetProcesses().Where(x => x.Id != 0 && x.Id != _currentProcess.Id).First();
        }

        [Test]
        public void FileIsLockedAfterPowerFailure_QueueObtainsLock()
        {
            //ARRANGE
            var queueName = GetQueueName();
            WriteLockFile(queueName, _otherProcess.Id, _currentThread, _otherProcess.StartTime.AddSeconds(1));

            //ACT
            using var queue = new PersistentQueue<string>(queueName);
        }

        [Test]
        public void FileIsLockedByCurentProcessButWrongThread_ThrowsException()
        {
            //ARRANGE
            var queueName = GetQueueName();
            WriteLockFile(queueName, _currentProcess.Id, 555, _currentProcess.StartTime);

            try
            {
                //ACT
                using var queue = new PersistentQueue<string>(queueName);
            }
            catch(InvalidOperationException ex)
            {
                //ASSERT
                Assert.AreEqual("This queue is locked by another thread in this process. Thread id = 555", ex.InnerException.Message);
                Assert.Pass();
            }
            Assert.Fail();
        }

        [Test]
        public void FileIsLockedByOtherRunningProcess_ThrowsException()
        {
            //ARRANGE
            var queueName = GetQueueName();
            WriteLockFile(queueName, _otherProcess.Id, 555, _otherProcess.StartTime);

            try
            {
                //ACT
                using var queue = new PersistentQueue<string>(queueName);
            }
            catch (InvalidOperationException ex)
            {
                //ASSERT
                Assert.AreEqual($"This queue is locked by another running process. Process id = {_otherProcess.Id}", ex.InnerException.Message);
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

        private void WriteLockFile(string queueName, int processId, int threadId, DateTime startTime)
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
