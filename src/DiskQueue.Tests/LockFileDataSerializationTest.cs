using DiskQueue.Implementation;
using NUnit.Framework;

namespace DiskQueue.Tests
{
    [TestFixture]
    internal class LockFileDataSerializationTest
    {
        [Test]
        public void ConvertToBytes_ExpectedOrderIsObtained()
        {
            var expected = new byte[]
            {
                8, 1, 0, 0,
                16, 2, 0, 0,
                32, 4, 0, 0, 0, 0, 0, 0
            };

            var data = new LockFileData
            {
                ProcessId = 8 + 256 * 1,
                ThreadId = 16 + 256 * 2,
                ProcessStart = 32 + 256 * 4
            };

            var actual = MarshallHelper.Serialize(data);
            for (var i = 0; i < expected.Length && i < actual.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [Test]
        public void ConvertFromBytes_MissingProcessStart_DeserializesCorrectly()
        {
            var input = new byte[]
            {
                8, 1, 0, 0,
                16, 2, 0, 0,
                //32, 4, 0, 0, 0, 0, 0, 0 // no ProcessStart bytes
            };

            var actual = MarshallHelper.Deserialize<LockFileData>(input);
            Assert.AreEqual(8 + 256 * 1, actual.ProcessId);
            Assert.AreEqual(16 + 256 * 2, actual.ThreadId);
            Assert.AreEqual(0, actual.ProcessStart);
        }

        [Test]
        public void ConvertFromBytes_CompleteData_DeserializesCorrectly()
        {
            var input = new byte[]
            {
                8, 1, 0, 0,
                16, 2, 0, 0,
                32, 4, 0, 0, 0, 0, 0, 0
            };

            var actual = MarshallHelper.Deserialize<LockFileData>(input);
            Assert.AreEqual(8 + 256 * 1, actual.ProcessId);
            Assert.AreEqual(16 + 256 * 2, actual.ThreadId);
            Assert.AreEqual(32 + 256 * 4, actual.ProcessStart);
        }
    }
}
