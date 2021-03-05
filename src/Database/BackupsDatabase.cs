using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using LiteDB;

namespace hlback.Database
{
	class BackupsDatabase : IDisposable
	{
		private enum DatabaseRecordFileValidity
		{
			Invalid,
			Nonmatch,
			ValidMatch
		}

		private const long TicksPerDay = 10000000L * 60 * 60 * 24;

		private const string DatabaseFileName = ".hlbackdatabase";
		private const string GroupCollectionName = "BackupFileGroups";

		private readonly string backupsRootPath;
		private readonly string databasePath;
		private readonly ConsoleOutput userInterface;

		private bool isDisposed = false;
		private LiteDatabase database;


		public BackupsDatabase(string backupsRootPath, ConsoleOutput userInterface)
		{
			this.backupsRootPath = backupsRootPath;
			this.databasePath = Path.Combine(backupsRootPath, DatabaseFileName);
			this.userInterface = userInterface;
			this.database = new LiteDatabase(databasePath);
		} // end Database constructor

		
		public HardLinkMatch getAvailableHardLinkMatch(string originFileHash, long originFileSize, DateTime originFileLastWriteTimeUTC,
			int? maxHardLinksPerFile, int? maxDataAgeForNewHardLink_Days)
		{
			// Variable to contain return value, defaulting to null.
			HardLinkMatch hardLinkMatch = null;

			// Translate the specified maximum age in days to a minimum date in ticks.
			long? minimumFileGroupCreationDate =
				(maxDataAgeForNewHardLink_Days == null) ? null : (DateTime.UtcNow.Ticks - maxDataAgeForNewHardLink_Days * TicksPerDay);

			// Obtain the collection of hard link groups from the databse.
			ILiteCollection<GroupRecord> backupFileGroupCollection = database.GetCollection<GroupRecord>(GroupCollectionName);

			// Get all hard link groups that are new enough to be used.
			// Order them based on most likelihood of being the best possible match:
			// starting with the least-used ones and, within that, newest ones first.
			IOrderedEnumerable<GroupRecord> potentialFileGroups =
				backupFileGroupCollection
					.Query()
					.Where(fileGroup => (fileGroup.Hash == originFileHash))
					.Where(fileGroup => (minimumFileGroupCreationDate == null || fileGroup.CreatedDate_UTC_Ticks >= minimumFileGroupCreationDate))
					.ToList()
					.OrderBy(fileGroup => fileGroup.Files.Count)
					.ThenByDescending(fileGroup => fileGroup.CreatedDate_UTC_Ticks);

			foreach(GroupRecord currentHardLinkGroupRecord in potentialFileGroups)
			{

				for (int i = currentHardLinkGroupRecord.Files.Count - 1; i >= 0; i--)
				{
					FileBackupRecord fileBackupRecordToCheck = currentHardLinkGroupRecord.Files[i];
					string lastValidPotentialMatch = null;
					string backupRecordFileFullPath = Path.Combine(backupsRootPath, fileBackupRecordToCheck.Path);

					DatabaseRecordFileValidity oldBackupFileMatchValidity =
						checkFileBackupRecord(currentHardLinkGroupRecord, fileBackupRecordToCheck, backupRecordFileFullPath, originFileSize, originFileLastWriteTimeUTC);

					if (oldBackupFileMatchValidity == DatabaseRecordFileValidity.ValidMatch)
					{
						// Database record points to a valid, matching previous backup file.
						// Remember this one as a possibly usable file to use as a hard link target.
						lastValidPotentialMatch = backupRecordFileFullPath;
					}
					else if (oldBackupFileMatchValidity == DatabaseRecordFileValidity.Invalid)
					{
						// The file referred to by this database record no longer exists or has been modified.
						// This database record is no longer valid, so delete the record.
						currentHardLinkGroupRecord.Files.RemoveAt(i);
						backupFileGroupCollection.Update(currentHardLinkGroupRecord);
					}
					else // Nonmatch
					{
						// Something about the file pointed to in this record doesn't match the origin file (e.g., last modified date).
						// Can't use this one (or others that are hard links of the same physical file) as a hard link source.
						// Stop looking at records in this group of identical hard links and move on to the next group.
						break;
					}

					// If we have found a match and haven't hit the max hard links allowed per physical copy, break out and stop looking for a link source.
					if (lastValidPotentialMatch != null && (maxHardLinksPerFile == null || currentHardLinkGroupRecord.Files.Count < maxHardLinksPerFile))
					{
						hardLinkMatch = new HardLinkMatch() { ID = currentHardLinkGroupRecord.ID, MatchingFilePath = lastValidPotentialMatch };
						break;
					}
				} // end for (int i = currentHardLinkGroupRecord.Files.Count - 1; i >= 0; i--)

				// If no records are left in this group, delete the whole group from the database.
				// If a match has been found, stop looking any further.
				if (currentHardLinkGroupRecord.Files.Count < 1)
					backupFileGroupCollection.Delete(currentHardLinkGroupRecord.ID);
				else if (hardLinkMatch != null)
					break;
			} // end foreach(GroupRecord currentHardLinkGroupRecord in potentialFileGroups)

			return hardLinkMatch;
		} // end getAvailableHardLinkMatch()


		public void addFileBackupRecord(string backedUpFileDestinationPath, long fileSize, string fileHash, DateTime fileLastModificationDateUTC, ObjectId existingHardLinkGroupID)
		{
			GroupRecord databaseRecord;

			// Obtain the collection of hard link groups from the databse.
			ILiteCollection<GroupRecord> backupFileGroupCollection = database.GetCollection<GroupRecord>(GroupCollectionName);

			FileBackupRecord newFileBackupRecord =
				new FileBackupRecord()
				{
					Path = backedUpFileDestinationPath.Substring(backupsRootPath.Length), // Store only the portion of the path below the root backups directory.
					LastModificationDate_UTC_Ticks = fileLastModificationDateUTC.Ticks
				};

			if (existingHardLinkGroupID == null)
			{
				List<FileBackupRecord> fileRecords = new List<FileBackupRecord>();
				fileRecords.Add(newFileBackupRecord);
				GroupRecord newRecord = new GroupRecord { Hash = fileHash, FileSize = fileSize, Files = fileRecords, CreatedDate_UTC_Ticks = DateTime.Now.ToUniversalTime().Ticks };
				backupFileGroupCollection.Insert(newRecord);
				backupFileGroupCollection.EnsureIndex(record => record.Hash);
			}
			else
			{
				databaseRecord = backupFileGroupCollection.FindById(existingHardLinkGroupID);
				databaseRecord.Files.Add(newFileBackupRecord);
				backupFileGroupCollection.Update(databaseRecord);
			}
		} // end addFileBackupRecord()


		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		} // end Dispose() [overload 1]


		protected virtual void Dispose(bool disposing)
		{
			if (isDisposed)
				return;
			
			if (disposing && database != null)
				database.Dispose();

			isDisposed = true;
		} // end Dispose() [overload 2]


		private DatabaseRecordFileValidity checkFileBackupRecord(
			GroupRecord groupRecordInfo, FileBackupRecord fileRecordInfo, string backupFilePath,
			long originFileSize, DateTime originFileLastWriteTimeUTC, string originFileHash = null)
		{
			FileInfo backupFile = new FileInfo(backupFilePath);

			if (!backupFile.Exists)
				return DatabaseRecordFileValidity.Invalid; // Record is invalid if the file doesn't still exist.
			else if (backupFile.LastWriteTimeUtc.Ticks != fileRecordInfo.LastModificationDate_UTC_Ticks)
				return DatabaseRecordFileValidity.Invalid; // Record is invalid if its last modification time is different from what was stored in the database.
			else if (backupFile.Length != groupRecordInfo.FileSize)
				return DatabaseRecordFileValidity.Invalid; // Record is invalid if the file size no longer matches what was stored in the database.
			else if (backupFile.Length != originFileSize || backupFile.LastWriteTimeUtc != originFileLastWriteTimeUTC)
				return DatabaseRecordFileValidity.Nonmatch; // The already-backed-up file doesn't match the origin file (e.g., different date because the origin is a copy). Return Nonmatch.
			else if (originFileHash != null && originFileHash != groupRecordInfo.Hash)
				return DatabaseRecordFileValidity.Invalid; // Function call specified checking the hash of the backed-up file, and it doesn't match what was in the database record.
			else
				return DatabaseRecordFileValidity.ValidMatch; // File record is valid and also matches the origin file.
		} // end checkFileBackupRecord()

	} // end class Database
}