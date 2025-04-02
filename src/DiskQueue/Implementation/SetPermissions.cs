using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using DiskQueue.Implementation.CrossPlatform.Unix;

namespace DiskQueue.Implementation
{
	/// <summary>
	/// File permission tools for Windows and Linux
	/// </summary>
	public static class SetPermissions
	{
		/// <summary>
		/// Set read-write access for all users, or ignore if not possible
		/// </summary>
		public static void TryAllowReadWriteForAll(string path)
		{
			if ( ! PersistentQueue.DefaultSettings.SetFilePermissions) return;
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

		private static void Ignore() { }

		private static void File_RWX_all(string path)
		{
			if (!OperatingSystem.IsWindows())
			{
				UnsafeNativeMethods.chmod(path, UnixFilePermissions.ACCESSPERMS);
			}
			else
			{
                var fileSecurity = new FileSecurity(path, AccessControlSections.All);
				var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null!);
				
                fileSecurity.SetAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.Modify | FileSystemRights.Synchronize, InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow));

                new FileInfo(path).SetAccessControl(fileSecurity);
			}
		}

		private static void Directory_RWX_all(string path)
		{
			if (!OperatingSystem.IsWindows())
			{
				UnsafeNativeMethods.chmod(path, UnixFilePermissions.ACCESSPERMS);
			}
			else
			{
                var directorySecurity = new DirectorySecurity(path, AccessControlSections.All);
                var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null!);
                directorySecurity.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.Modify | FileSystemRights.Synchronize, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));

                new DirectoryInfo(path).SetAccessControl(directorySecurity);
			}
		}
	}
}