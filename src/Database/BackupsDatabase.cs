using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using LiteDB;

namespace hlback.Database
{
	// BackupsDatabase:
	/// <summary>Class which manages storage and retrieval from the [LiteDB] backups database. [implements IDisposable]</summary>
	class BackupsDatabase : IDisposable
	{
		#region Private Member Variables (Immutable)

		/// <summary>Used for comparison of file dates.</summary>
		private const long TicksPerDay = 10000000L * 60 * 60 * 24;

		/// <summary>Name of the file used for the backups database.</summary>
		private const string DatabaseFileName = ".hlbackdatabase";
		
		/// <summary>Name of the collection used in the LiteDB database for storing collections of identical files sharing a single physical copy.</summary>
		private const string GroupCollectionName = "BackupFileGroups";

		/// <summary>Stores the destination root path of the backup job.</summary>
		private readonly string backupsRootPath;
		
		/// <summary>Stores the path (including filename) of the backups database.</summary>
		private readonly string databasePath;
		
		/// <summary>Stores an object used for reporting output to the user.</summary>
		private readonly ConsoleOutput userInterface;

		#endregion


		#region Private Member Variables (Mutable)

		/// <summary>Tracks whether this object has been disposed.</summary>
		private bool isDisposed = false;

		/// <summary>Object referencing the LiteDB database itself.</summary>
		private LiteDatabase database;

		#endregion


		#region Public Methods

		// BackupsDatabase constructor:
		/// <summary>Initializes the BackupsDatabase object and creates or connects to the database file.</summary>
		/// <param name="backupsRootPath">The base directory for backups, in which the database either exists or will be created.</param>
		/// <param name="userInterface">An object for use in producing output for the user.</param>
		public BackupsDatabase(string backupsRootPath, ConsoleOutput userInterface)
		{
			this.backupsRootPath = backupsRootPath;
			this.databasePath = Path.Combine(backupsRootPath, DatabaseFileName);
			this.userInterface = userInterface;
			this.database = new LiteDatabase(databasePath); // CHANGE CODE HERE: handle exceptions
		} // end Database constructor

		
		// getAvailableHardLinkMatch():
		/// <summary>
		/// 	Attempts to find a match, among previously backed-up files, for the specified file hash, size, and last modification date.
		/// 	Allows limiting matches by age and by number of other files already grouped with the matching file.
		/// </summary>
		/// <returns>A <c>HardLinkMatch</c> object with info about the matching file if one is found, or <c>null</c> if no match is found.</returns>
		/// <param name="originFileHash">Hash of the file to be matched.</param>
		/// <param name="originFileSize">Size, in bytes, of the file to be matched.</param>
		/// <param name="originFileLastWriteTimeUTC">Modification date of the file to be matched.</param>
		/// <param name="maxHardLinksPerFile">Maximum number of matches allowed in a single file group.</param>
		/// <param name="maxDataAgeForNewHardLink_Days">Maximum age of a file allowed to be used as a match.</param>
		public HardLinkMatch getAvailableHardLinkMatch(string originFileHash, long originFileSize, DateTime originFileLastWriteTimeUTC,
			int? maxHardLinksPerFile, int? maxDataAgeForNewHardLink_Days)
		{
			// Variable to contain return value, defaulting to null.
			HardLinkMatch hardLinkMatch = null;

			// Translate the specified maximum age in days to a minimum date in ticks.
			long? minimumFileGroupCreationDate =
				(maxDataAgeForNewHardLink_Days == null) ? null : (DateTime.UtcNow.Ticks - maxDataAgeForNewHardLink_Days * TicksPerDay);

			// Obtain the collection of hard link groups from the databse.
			// CHANGE CODE HERE: Handle exceptions
			ILiteCollection<GroupRecord> backupFileGroupCollection = database.GetCollection<GroupRecord>(GroupCollectionName);

			// Get all hard link groups that match the hash and are new enough to be used.
			// Order them based on most likelihood of being the best possible match:
			// starting with the least-used ones and, within that, newest ones first.
			// CHANGE CODE HERE: Handle exceptions
			IOrderedEnumerable<GroupRecord> potentialFileGroups =
				backupFileGroupCollection
					.Query()
					.Where(fileGroup => (fileGroup.Hash == originFileHash))
					.Where(fileGroup => (minimumFileGroupCreationDate == null || fileGroup.CreatedDate_UTC_Ticks >= minimumFileGroupCreationDate))
					.ToList()
					.OrderBy(fileGroup => fileGroup.Files.Count)
					.ThenByDescending(fileGroup => fileGroup.CreatedDate_UTC_Ticks);

			// Iterate through each file group having a matching hash as found above,
			// checking for any files within that group that can be returned as a match.
			foreach (GroupRecord currentHardLinkGroupRecord in potentialFileGroups)
			{
				// Look at each file in this group (until we find a match). For each, verify the file still exists and is unchanged.
				// If it is changed or missing, delete its record from the file group in the database.
				// If one is verified and the number in the group is below the maximum (or there is no maximum),
				// or the group size drops below the maximum due to cleaning out no-longer-valid records, choose that one to return.
				for (int i = currentHardLinkGroupRecord.Files.Count - 1; i >= 0; i--)
				{
					// Get the current file to look at.
					FileBackupRecord fileBackupRecordToCheck = currentHardLinkGroupRecord.Files[i];

					// Keep a record of the last verified file match in this group that was found.
					string lastValidPotentialMatch = null;

					// Figure out the full path to the file recorded in the databse.
					string backupRecordFileFullPath = Path.Combine(backupsRootPath, fileBackupRecordToCheck.Path); // CHANGE CODE HERE: Handle exceptions

					if (verifyBackedUpFileAgainstRecord(currentHardLinkGroupRecord, fileBackupRecordToCheck, backupsRootPath))
					{
						// Database record still matches the previous backup file (backed-up file hasn't been modified or deleted since the record was made).
						// Now make sure it matches the origin file with respect to size and modification date.
						// (A size mismatch would be highly unlikely and result only from a hash collision.
						// But a modification date mismatch could occur if a file is changed and then changed back.
						// Because we want to preserve file dates in the backup copies, we will consider that a mismatch--
						// don't want to "back up" a file by creating a hard link to another file with the same contents but a different date.)
						if (fileMatchesSizeAndDate(backupRecordFileFullPath, originFileSize, originFileLastWriteTimeUTC))
						{
							// Database record points to a valid, matching previous backup file.
							// Remember this one as a possibly usable file to use as a hard link target.
							lastValidPotentialMatch = backupRecordFileFullPath;
						}
						else
						{
							// Something about the file pointed to in this record doesn't match the origin file (e.g., last modified date).
							// Can't use this one (or others that are hard links of the same physical file) as a hard link source.
							// Stop looking at records in this group of identical hard links and move on to the next group.
							break;
						}
					}
					else
					{
						// The file referred to by this database record no longer exists or has been modified.
						// This database record is no longer valid, so delete the record.
						currentHardLinkGroupRecord.Files.RemoveAt(i);
						backupFileGroupCollection.Update(currentHardLinkGroupRecord); // CHANGE CODE HERE: Handle exceptions
					}

					// If we have found a match and are under the max hard links allowed per physical copy, break out and stop looking for a link source.
					if (lastValidPotentialMatch != null && (maxHardLinksPerFile == null || currentHardLinkGroupRecord.Files.Count < maxHardLinksPerFile))
					{
						hardLinkMatch = new HardLinkMatch() { ID = currentHardLinkGroupRecord.ID, MatchingFilePath = lastValidPotentialMatch };
						break;
					}
				} // end for (int i = currentHardLinkGroupRecord.Files.Count - 1; i >= 0; i--)

				// If no records are left in this group, delete the whole group from the database.
				// If a match has been found, stop looking any further.
				if (currentHardLinkGroupRecord.Files.Count < 1)
					backupFileGroupCollection.Delete(currentHardLinkGroupRecord.ID); // CHANGE CODE HERE: Handle exceptions
				else if (hardLinkMatch != null)
					break;
			} // end foreach(GroupRecord currentHardLinkGroupRecord in potentialFileGroups)

			return hardLinkMatch;
		} // end getAvailableHardLinkMatch()


		// addFileBackupRecord():
		/// <summary>Adds a new database record for a backed-up file.</summary>
		/// <param name="backedUpFileDestinationPath">Full path of the file being recorded.</param>
		/// <param name="fileSize">Size, in bytes, of the file being recorded.</param>
		/// <param name="fileHash">Hash string of the contents of the file being recorded.</param>
		/// <param name="fileLastModificationDateUTC">Last changed date of the file being recorded.</param>
		/// <param name="existingHardLinkGroupID">ID of the existing file group into which this file should be put, or <c>null</c> to put it in a new group.</param>
		public void addFileBackupRecord(string backedUpFileDestinationPath, long fileSize, string fileHash, DateTime fileLastModificationDateUTC, ObjectId existingHardLinkGroupID)
		{
			// Obtain the collection of hard link groups from the database.
			// CHANGE CODE HERE: handle exceptions
			ILiteCollection<GroupRecord> backupFileGroupCollection = database.GetCollection<GroupRecord>(GroupCollectionName);

			// Create a new FileBackupRecord object to store in the database.
			FileBackupRecord newFileBackupRecord =
				new FileBackupRecord()
				{
					Path = backedUpFileDestinationPath.Substring(backupsRootPath.Length), // Store only the portion of the path below the root backups directory.
					LastModificationDate_UTC_Ticks = fileLastModificationDateUTC.Ticks
				};

			if (existingHardLinkGroupID == null)
			{
				// No existing group was specified. We will create a new file group in the database to put this file in.
				// Set up a new list of files, and insert it along with all the other needed data into a new GroupRecord object.
				List<FileBackupRecord> fileRecords = new List<FileBackupRecord>();
				fileRecords.Add(newFileBackupRecord);
				GroupRecord newRecord = new GroupRecord { Hash = fileHash, FileSize = fileSize, Files = fileRecords, CreatedDate_UTC_Ticks = DateTime.Now.ToUniversalTime().Ticks };

				// Insert the new group into the database.
				backupFileGroupCollection.Insert(newRecord); // CHANGE CODE HERE: handle exceptions

				// Make sure the database is indexed on the file hash value.
				backupFileGroupCollection.EnsureIndex(record => record.Hash); // CHANGE CODE HERE: handle exceptions
			}
			else
			{
				// An existing group ID was specified. We will insert the file into that group.
				
				// Get the existing database record for the group.
				GroupRecord databaseRecord = backupFileGroupCollection.FindById(existingHardLinkGroupID);

				// Add this file to that group's Files collection, and update the database.
				databaseRecord.Files.Add(newFileBackupRecord);
				backupFileGroupCollection.Update(databaseRecord); // CHANGE CODE HERE: handle exceptions
			}
		} // end addFileBackupRecord()


		// Dispose():
		/// <summary>Public disposer method for the class. Disposes of unmanaged resources.</summary>
		public void Dispose()
		{
			// Call the version of Dispose() that does the work, and suppress finalization.
			Dispose(true);
			GC.SuppressFinalize(this);
		} // end Dispose() [overload 1]

		#endregion


		#region Protected and Private Methods

		// Dispose():
		/// <summary>Disposer method for the class. Disposes of unmanaged resources.</summary>
		/// <param name="disposing">Indicates whether or not a deterministic disposal process is underway.</param>
		protected virtual void Dispose(bool disposing)
		{
			// Nothing to do if the object is already disposed.
			if (isDisposed)
				return;
			
			// Dispose of the one resource held by this class which needs it: the database object,
			// if disposal is happening deterministically-- that is, not from a finalizer during garbage collection,
			// since that might already have or will be going to dispose of the database object.
			if (disposing && database != null)
				database.Dispose();

			// Record that we've already done this, so we don't do it again.
			isDisposed = true;
		} // end Dispose() [overload 2]


		// verifyBackedUpFileAgainstRecord():
		/// <summary>Makes sure a file still matches the info recorded about it in the database.</summary>
		/// <returns>A <c>bool</c> indicating whether the file record still matches the file to which it corresponds.</returns>
		/// <param name="groupRecordInfo">The group object to which the file belongs.</param>
		/// <param name="fileRecordInfo">The file record object corresponding to the file in question.</param>
		/// <param name="basePath">The base path with which the file record's [relative] path should be combined to find the file on disk.</param>
		private bool verifyBackedUpFileAgainstRecord(GroupRecord groupRecordInfo, FileBackupRecord fileRecordInfo, string basePath)
		{
			FileInfo backedUpFile = new FileInfo(Path.Combine(basePath, fileRecordInfo.Path)); // CHANGE CODE HERE: Handle exceptions

			if (!backedUpFile.Exists)
				return false; // Record is invalid because the previously backed-up file doesn't still exist.
			else if (backedUpFile.LastWriteTimeUtc.Ticks != fileRecordInfo.LastModificationDate_UTC_Ticks) // CHANGE CODE HERE: Handle exceptions
				return false; // Record is invalid because the previously backed-up file's modification time indicates it has been changed since being put there.
			else if (backedUpFile.Length != groupRecordInfo.FileSize) // CHANGE CODE HERE: Handle exceptions
				return false; // Record is invalid because the previously backed-up file size no longer matches what was stored in the database.

			return true; // File record matches the backed-up file.
		} // end verifyBackedUpFileAgainstRecord()


		// fileMatchesSizeAndDate():
		/// <summary>Checks that the file at a given path is of the specified size and was last modified at the specified date and time.</summary>
		/// <returns>A <c>bool</c> indicating whether file matches the size and date given.</returns>
		/// <param name="filePath">Full path of the file to examine.</param>
		/// <param name="size">Expected size of the file, in bytes.</param>
		/// <param name="lastWriteTimeUTC">Expected last modification date of the file, in UTC.</param>
		private bool fileMatchesSizeAndDate(string filePath, long size, DateTime lastWriteTimeUTC)
		{
			// Get a reference to the file, and compare its size and last write date to the expected values.
			// If either does not match, return false; otherwise, return true.
			// CHANGE CODE HERE: Handle exceptions in the below
			FileInfo fileToCheck = new FileInfo(filePath);
			if (fileToCheck.Length != size)
				return false;
			else if (fileToCheck.LastWriteTimeUtc != lastWriteTimeUTC)
				return false;
			
			return true;
		} // end fileMatchesSizeAndDate()

		#endregion

	} // end class Database
}