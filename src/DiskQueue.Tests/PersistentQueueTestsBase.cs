using System;
using NUnit.Framework;
using System.IO;

namespace DiskQueue.Tests
{
	public class PersistentQueueTestsBase
	{
		protected const string path = @"./queue";
		static readonly object _lock = new Object();

		[SetUp]
		public void Setup()
		{
			RebuildPath();
		}

		/// <summary>
		/// This ensures that we release all files before we complete a test
		/// </summary>
		[TearDown]
		public void Teardown()
		{
			RebuildPath();
		}

		static void RebuildPath()
		{
			lock (_lock)
			{
				try
				{
					if (Directory.Exists(path))
					{
						var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
						Array.Sort(files, (s1, s2) => s2.Length.CompareTo(s1.Length)); // sortby length descending
						foreach (var file in files)
						{
							File.Delete(file);
						}

						Directory.Delete(path, true);

					}
					//Directory.CreateDirectory(path);
				}
				catch (UnauthorizedAccessException)
				{
					Console.WriteLine("Not allowed to delete queue directory. May fail later");
				}
			}
		}
	}
}