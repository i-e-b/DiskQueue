using System;

namespace DiskQueue.Implementation.CrossPlatform.Unix
{
	/// <summary>
	/// Unix file system permission flags
	/// </summary>
	[Flags]
	public enum UnixFilePermissions : uint
	{
		/// <summary> Set-user-ID on execution </summary>
		S_ISUID = 2048u,

		/// <summary> Set-group-ID on execution </summary>
		S_ISGID = 1024u,

		/// <summary> On directories, restricted deletion flag </summary>
		S_ISVTX = 512u,

		/// <summary> Read permission, owner </summary>
		S_IRUSR = 256u,

		/// <summary> Write permission, owner </summary>
		S_IWUSR = 128u,

		/// <summary> Execute/search permission, owner </summary>
		S_IXUSR = 64u,

		/// <summary> Read permission, group </summary>
		S_IRGRP = 32u,

		/// <summary> Write permission, group </summary>
		S_IWGRP = 16u,

		/// <summary> Execute/search permission, group </summary>
		S_IXGRP = 8u,

		/// <summary> Read permission, others </summary>
		S_IROTH = 4u,

		/// <summary> Write permission, others </summary>
		S_IWOTH = 2u,

		/// <summary> Execute/search permission, others </summary>
		S_IXOTH = 1u,

		/// <summary> Read, write, search and execute, group </summary>
		S_IRWXG = 56u,

		/// <summary> Read, write, search and execute, owner </summary>
		S_IRWXU = 448u,

		/// <summary> Read, write, search and execute, others </summary>
		S_IRWXO = 7u,

		
		/// <summary> Read, write, search and execute for owner, group and others </summary>
		ACCESSPERMS = 511u,
		
		/// <summary> Restrict delete, set all flags on execute, all permissions to all users </summary>
		ALLPERMS = 4095u,
		
		/// <summary> Read and write, no execute for owner, group and others </summary>
		DEFFILEMODE = 438u,
		
		/// <summary> Type of file flag </summary>
		S_IFMT = 61440u,
		
		/// <summary> Directory type </summary>
		S_IFDIR = 16384u,
		
		/// <summary> Character special file type </summary>
		S_IFCHR = 8192u,
		
		/// <summary> Block special file type </summary>
		S_IFBLK = 24576u,
		
		/// <summary> Regular file type </summary>
		S_IFREG = 32768u,
		
		/// <summary> FIFO special file type </summary>
		S_IFIFO = 4096u,
		
		/// <summary> Symbolic link file type </summary>
		S_IFLNK = 40960u,
		
		/// <summary> Socket file type </summary>
		S_IFSOCK = 49152u
	}
}