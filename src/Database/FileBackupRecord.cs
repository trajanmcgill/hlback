using System;

namespace hlback.Database
{
	class FileBackupRecord
	{
		public string Path { get; init; }
		public long LastModificationDate_UTC_Ticks { get; init; }
	} // end class FileBackupRecord
}