using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using DiskQueue.Implementation.CrossPlatform.Unix;

namespace DiskQueue
{
	public static class SetPermissions
	{
		public static bool RunningUnderPosix
		{
			get
			{
				var p = (int)Environment.OSVersion.Platform;
				return (p == 4) || (p == 6) || (p == 128);
			}
		}

		/// <summary>
		/// Set read-write access for all users, or throw an exception
		/// if not possible
		/// </summary>
		public static void AllowReadWriteForAll(string path)
		{
			if (Directory.Exists(path)) DirectoryRWall(path);
			else if (File.Exists(path)) FileRWall(path);
			else throw new UnauthorizedAccessException("Can't access the path \"" + path + "\"");
		}

		/// <summary>
		/// Set read-write access for all users, or ignore if not possible
		/// </summary>
		public static void TryAllowReadWriteForAll(string path)
		{
			try
			{
				if (Directory.Exists(path)) DirectoryRWall(path);
				else if (File.Exists(path)) FileRWall(path);
			}
			catch
			{
				Ignore();
			}
		}

		static void Ignore() { }

		static void FileRWall(string path)
		{
			if (RunningUnderPosix)
			{
				CoreUnixCalls.chmod(path, UnixFilePermissions.DEFFILEMODE);
			}
			else
			{
				var sec = File.GetAccessControl(path);
				var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
				sec.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.Modify | FileSystemRights.Synchronize, InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow));
				File.SetAccessControl(path, sec);
			}
		}

		static void DirectoryRWall(string path)
		{
			if (RunningUnderPosix)
			{
				CoreUnixCalls.chmod(path, UnixFilePermissions.DEFFILEMODE);
			}
			else
			{
				var sec = Directory.GetAccessControl(path);
				var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
				sec.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.Modify | FileSystemRights.Synchronize, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
				Directory.SetAccessControl(path, sec);
			}
		}
	}
}