using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using DiskQueue.Implementation;
using NUnit.Framework;

namespace DiskQueue.Tests
{
    [TestFixture]
    public class MemoryMappedFileDriverTests : PersistentQueueTestsBase
    {
        protected override string Path => "./MemMapFileTests";

        [Test]
        public void mini_test_delete_me()
        {
            const string path = @"C:\Temp\minitest3.dat";
            if (!File.Exists(path)) File.WriteAllBytes(path, new byte[4]);
            
            var b1 = File.ReadAllBytes(path);
            var v1 = (b1[3] << 24) + (b1[2] << 16) + (b1[1] << 8) + b1[0];
            Console.WriteLine($"v@1={v1}");

            //using (var mmf1 = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 8)) // will grow file if needed
            using (var mmf1 = MemoryMappedFile.CreateFromFile(path, FileMode.Open)) // will grow file if needed
            {
                using var accessor2 = mmf1.CreateViewStream();
                Console.WriteLine($"pre pos={accessor2.Position}");
                accessor2.Write(new byte[]{1,2,3,4,5,6}); // this truncates if file is not long enough
                Console.WriteLine($"post pos={accessor2.Position}");
                accessor2.Write(new byte[]{1,2,3,4,5,6}); // this truncates if file is not long enough
                Console.WriteLine($"post pos2={accessor2.Position}"); // just keeps on writing into empty space
                accessor2.Flush();
                /*using var accessor1 = mmf1.CreateViewAccessor(offset: 0, size: 8); // must be less or equal to file size

                var ptr = accessor1.SafeMemoryMappedViewHandle.DangerousGetHandle();
                unsafe
                {
                    ref int x = ref *(int*)ptr;

                    Console.WriteLine($"x@1={x}");
                    Interlocked.Increment(ref x);
                    Console.WriteLine($"x@2={x}");
                }

                //using var mmf2 = MemoryMappedFile.CreateFromFile(path, FileMode.Open); // should throw System.IO.IOException : The process cannot access the file '...' because it is being used by another process.
                accessor1.Flush();

                var value = accessor1.ReadInt32(0);
                Console.WriteLine($"Value={value}");
                */
            }

            b1 = File.ReadAllBytes(path);
            Console.WriteLine($"File is {b1.Length} bytes");
            var v3 = (b1[3] << 24) + (b1[2] << 16) + (b1[1] << 8) + b1[0];
            Console.WriteLine($"v@3={v3}");
        }

        [Test]
        public void basic_test_of_file_driver ()
        {
            using var subject = new PersistentQueue(Path);
            subject.HardDelete(true);
            subject.Internals.SetFileDriver(new MemoryMappedFileDriver());
            
            int dequeues = 0;
            int enqueues = 0;

            using (var session = subject.OpenSession())
            {
                for (int i = 0; i < 100; i++)
                {
                    session.Enqueue(new byte[] { 1, 2, 3, 4 });
                    session.Flush();
                    enqueues++;
                }
            }
            
            using (var session = subject.OpenSession())
            {
                for (int i = 0; i < 100; i++)
                {
                    var data = session.Dequeue();
                    if (data is not null) dequeues++;
                    session.Flush();
                }
            }
            
            // Restore driver so we can dispose correctly.
            subject.Internals.SetFileDriver(new StandardFileDriver());
            
            Assert.That(dequeues, Is.EqualTo(100), "dequeue count");
            Assert.That(enqueues, Is.EqualTo(100), "enqueue count");
        }
    }
}