using System;
using System.Runtime.InteropServices;

namespace DiskQueue.Implementation.CrossPlatform.Unix
{
	[Flags]
	public enum UnixFilePermissions : uint
	{
		S_ISUID = 2048u,
		S_ISGID = 1024u,
		S_ISVTX = 512u,
		S_IRUSR = 256u,
		S_IWUSR = 128u,
		S_IXUSR = 64u,
		S_IRGRP = 32u,
		S_IWGRP = 16u,
		S_IXGRP = 8u,
		S_IROTH = 4u,
		S_IWOTH = 2u,
		S_IXOTH = 1u,
		S_IRWXG = 56u,
		S_IRWXU = 448u,
		S_IRWXO = 7u,
		ACCESSPERMS = 511u,
		ALLPERMS = 4095u,
		DEFFILEMODE = 438u,
		S_IFMT = 61440u,
		S_IFDIR = 16384u,
		S_IFCHR = 8192u,
		S_IFBLK = 24576u,
		S_IFREG = 32768u,
		S_IFIFO = 4096u,
		S_IFLNK = 40960u,
		S_IFSOCK = 49152u
	}
	public class CoreUnixCalls
	{
		[DllImport("libc", EntryPoint = "chmod", SetLastError = true)]
		static extern int sys_chmod(string path, uint mode);

		public static int chmod(string path, UnixFilePermissions mode)
		{
			return sys_chmod(path, (uint)mode);
		} 
	}
}