using System;

namespace hlback.Database
{
	class FileBackupRecord
	{
		public string Hash { get; init; }
		public long PhysicalCopyGroup { get; init; }
		public string Path { get; init; }
		public long Size { get; init; }
		public DateTime LastModificationDate { get; init; }
	} // end class FileBackupRecord
}