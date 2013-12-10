using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using DiskQueue.Implementation.CrossPlatform.Unix;

namespace DiskQueue
{
	/// <summary>
	/// File permission tools for Windows and Linux
	/// </summary>
	public static class SetPermissions
	{
		/// <summary>
		/// True if running in a Posix environment, false if Windows or unknown.
		/// </summary>
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
			if (Directory.Exists(path)) Directory_RWX_all(path);
			else if (File.Exists(path)) File_RWX_all(path);
			else throw new UnauthorizedAccessException("Can't access the path \"" + path + "\"");
		}

		/// <summary>
		/// Set read-write access for all users, or ignore if not possible
		/// </summary>
		public static void TryAllowReadWriteForAll(string path)
		{
			try
			{
				if (Directory.Exists(path)) Directory_RWX_all(path);
				else if (File.Exists(path)) File_RWX_all(path);
			}
			catch
			{
				Ignore();
			}
		}

		static void Ignore() { }

		static void File_RWX_all(string path)
		{
			if (RunningUnderPosix)
			{
				UnsafeNativeMethods.chmod(path, UnixFilePermissions.ACCESSPERMS);
			}
			else
			{
				var sec = File.GetAccessControl(path);
				var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
				sec.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.Modify | FileSystemRights.Synchronize, InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow));
				File.SetAccessControl(path, sec);
			}
		}

		static void Directory_RWX_all(string path)
		{
			if (RunningUnderPosix)
			{
				UnsafeNativeMethods.chmod(path, UnixFilePermissions.ACCESSPERMS);
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