// Copyright (c) 2005 - 2008 Ayende Rahien (ayende@ayende.com)
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice,
//     this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice,
//     this list of conditions and the following disclaimer in the documentation
//     and/or other materials provided with the distribution.
//     * Neither the name of Ayende Rahien nor the names of its
//     contributors may be used to endorse or promote products derived from this
//     software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
// THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.IO;
using System.Threading;

namespace DiskQueue.Implementation
{
	/// <summary>
	/// Allow to overwrite a file in a transactional manner.
	/// That is, either we completely succeed or completely fail in writing to the file.
	/// Read will correct previous failed transaction if a previous write has failed.
	/// Assumptions:
	///  * You want to always rewrite the file, rathar than edit it.
	///  * The underlying file system has at least transactional metadata.
	///  * Thread safety is provided by the calling code.
	/// 
	/// Write implementation:
	///  - Rename file to "[file].old_copy" (overwrite if needed
	///  - Create new file stream with the file name and pass it to the client
	///  - After client is done, close the stream
	///  - Delete old file
	/// 
	/// Read implementation:
	///  - If old file exists, remove new file and rename old file
	/// 
	/// </summary>
	public static class Atomic
	{
		static readonly object _lock = new object();
		
		/// <summary>
		/// Run a read action over a file by name.
		/// Access is optimised for sequential scanning.
		/// No file share is permitted.
		/// </summary>
		/// <param name="path">File path to read</param>
		/// <param name="action">Action to consume file stream. You do not need to close the stream yourself.</param>
		public static void Read(string path, Action<Stream> action)
		{
			lock (_lock)
			{
				if (File.Exists(path + ".old_copy"))
				{
					if (WaitDelete(path))
						File.Move(path + ".old_copy", path);
				}

				using (
					var stream = new FileStream(path,
					                            FileMode.OpenOrCreate,
					                            FileAccess.Read,
					                            FileShare.None,
					                            0x10000,
					                            FileOptions.SequentialScan)
					)
				{
					SetPermissions.TryAllowReadWriteForAll(path);
					action(stream);
				}
			}
		}

		/// <summary>
		/// Run a write action to a file.
		/// This will always rewrite the file (no appending).
		/// </summary>
		/// <param name="path">File path to write</param>
		/// <param name="action">Action to write into file stream. You do not need to close the stream yourself.</param>
		public static void Write(string path, Action<Stream> action)
		{
			lock (_lock)
			{
				// if the old copy file exists, this means that we have
				// a previous corrupt write, so we will not overrite it, but 
				// rather overwrite the current file and keep it as our backup.
				if (File.Exists(path + ".old_copy") == false)
					File.Move(path, path + ".old_copy");

				using (
					var stream = new FileStream(path,
					                            FileMode.Create,
					                            FileAccess.Write,
					                            FileShare.None,
					                            0x10000,
					                            FileOptions.WriteThrough | FileOptions.SequentialScan)
					)
				{
					SetPermissions.TryAllowReadWriteForAll(path);
					action(stream);
					stream.Flush();
				}
				
				WaitDelete(path + ".old_copy");
			}
		}

		static bool WaitDelete(string s)
		{
			for (int i = 0; i < 5; i++)
			{
				try
				{
					File.Delete(s);
					return true;
				}
				catch
				{
					Thread.Sleep(100);
				}
			}
			return false;
		}
	}
}