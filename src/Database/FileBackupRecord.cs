using System;

namespace hlback.Database
{
	// FileBackupRecord:
	/// <summary>Class corresponding to the record of an individual file as stored in the database.</summary>
	class FileBackupRecord
	{
		/// <summary>Stored path of the file.</summary>
		public string Path { get; init; }

		/// <summary>Last modification date of the file, at the time the record is created.</summary>
		public long LastModificationDate_UTC_Ticks { get; init; }
	} // end class FileBackupRecord
}