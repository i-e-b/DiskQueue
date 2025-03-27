using System;
using NUnit.Framework;
// ReSharper disable AssignNullToNotNullAttribute

namespace DiskQueue.Tests
{
    [TestFixture, SingleThreaded]
    public class GenericQueueTests
    {
        // Note: having the queue files shared between the tests checks that we 
        // are correctly closing down the queue (i.e. the `Dispose()` call works)
        // If not, one of the files will fail complaining that the lock is still held.
        private const string QueueName = "./GenericQueueTest";
        
        [Test]
        public void Round_trip_value_type()
        {
            using var queue = new PersistentQueue<int>(QueueName+"int"); 
            using var session = queue.OpenSession();

            session.Enqueue(7);
            session.Flush();
            var testNumber = session.Dequeue();
            session.Flush();
            Assert.AreEqual(7, testNumber);
        }

        [TestCase("Test")]
        [TestCase("")]
        [TestCase(" Leading Spaces")]
        [TestCase("Trailing Spaces   ")]
        [TestCase("A string longer than the others but still quite short")]
        [TestCase("other \r\n\t\b characters")]
        public void Round_trip_string_type(string valueToTest)
        {
            // Use different queue for each test case so that we don't get errors when running tests concurrently.
            var hash = valueToTest.GetHashCode().ToString("X8");
            using var queue = new PersistentQueue<string>($"./GenericQueueTests3{hash}");
            using var session = queue.OpenSession();

            while (session.Dequeue() != null) { Console.WriteLine("Removing old data"); }
            session.Flush();

            session.Enqueue(valueToTest);
            session.Flush();
            var returnValue = session.Dequeue();
            session.Flush();
            Assert.AreEqual(valueToTest, returnValue);
        }

        [Test]
        public void Round_trip_complex_type()
        {
            using var queue = new PersistentQueue<TestClass>(QueueName+"TC");
            using var session = queue.OpenSession();
            
            var testObject = new TestClass(7, "TestString", null);
            session.Enqueue(testObject);
            session.Flush();
            var testObject2 = session.Dequeue();
            session.Flush();
            Assert.IsNotNull(testObject2);
            Assert.AreEqual(testObject,testObject2);

            testObject = new TestClass(7, "TestString", -5);
            session.Enqueue(testObject);
            session.Flush();
            testObject2 = session.Dequeue();
            session.Flush();
            Assert.IsNotNull(testObject2);
            Assert.AreEqual(testObject, testObject2);
        }

        [Test]
        public void Round_trip_DateTimeOffset()
        {
            using var queue   = new PersistentQueue<DateTimeOffset>(QueueName+"TC2");
            using var session = queue.OpenSession();

            var testObject = DateTimeOffset.Now;
            session.Enqueue(testObject);
            session.Flush();
            var testObject2 = session.Dequeue();
            session.Flush();
            Assert.IsNotNull(testObject2);
            Assert.AreEqual(testObject,testObject2);
        }
    }

    /// <summary>
    /// Test class for tests on <see cref="PersistentQueue{T}"/>
    /// </summary>
    [Serializable]
    public class TestClass : IEquatable<TestClass>
    {
        public TestClass(int integerValue, string stringValue, int? nullableIntegerValue)
        {
            IntegerValue = integerValue;
            StringValue = stringValue;
            NullableIntegerValue = nullableIntegerValue;
            TimeOffset = DateTimeOffset.Now;
            Time = DateTime.Now;
        }

        public int IntegerValue { get; }
        public string StringValue { get; }
        public int? NullableIntegerValue { get; }
        public DateTimeOffset TimeOffset { get; }
        public DateTime Time { get; set; }

        public bool Equals(TestClass? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return IntegerValue == other.IntegerValue
                && StringValue == other.StringValue
                && NullableIntegerValue == other.NullableIntegerValue
                && (TimeOffset - other.TimeOffset).Duration() < TimeSpan.FromSeconds(1)
                && (Time - other.Time).Duration() < TimeSpan.FromSeconds(1);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TestClass)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(IntegerValue, StringValue, NullableIntegerValue);
        }

        public static bool operator ==(TestClass left, TestClass right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TestClass left, TestClass right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Override ToString so that the contents can easily be inspected in the debugger.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{IntegerValue}|{StringValue}|{NullableIntegerValue}";
        }
    }
}