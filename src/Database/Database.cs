using System;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace hlback.Database
{
	class Database
	{
		private enum DatabaseRecordFileValidity
		{
			Invalid,
			Nonmatch,
			ValidMatch
		}


		private const string TimestampFileNameCreationPattern = "yyyy-MM-dd.HH-mm-ss.fff";
		private const string TimestampFileNameFindPattern =     "????-??-??.??-??-??.???.*";
		private const string TimestameFileNameFindPatternRE =  @"^(\d\d\d\d)-(\d\d)-(\d\d)\.(\d\d)-(\d\d)-(\d\d)\.(\d\d\d).(\d?)$";
		private const int TimeStampREGroup_Year = 1, TimeStampREGroup_Month = 2, TimeStampREGroup_Day = 3,
			TimeStampREGroup_Hour = 4, TimeStampREGroup_Minute = 5, TimeStampREGroup_Second = 6, TimeStampREGroup_Millisecond = 7,
			TimeStampREGroup_Counter = 8; 

		private const string DatabaseDirectoryName = ".hlbackdatabase";
		private readonly Encoding DatabaseFileEncoding = Encoding.UTF8;
		
		private readonly int HashLength;
		private readonly DirectoryInfo databaseDirectory;
		private readonly ConsoleOutput userInterface;


		public Database(string backupsRootPath, ConsoleOutput userInterface)
		{
			HashLength = getHash("hashtest").Length;
			databaseDirectory = new DirectoryInfo(Path.Combine(backupsRootPath, DatabaseDirectoryName));
			this.userInterface = userInterface;
		} // end Database constructor

		
		public DatabaseQueryResults getDatabaseInfoForFile(FileInfo originFile, int? maxHardLinksPerFile, int? maxDataAgeForNewHardLink_Days, string currentBackupTimestampString)
		{
			string newRecordPath, newRecordFileName;
			(FileInfo targetFile, DirectoryInfo databaseRecordGroup) bestHardLinkTarget;

			// Hash the origin file, and find out the base location for database records for files with this hash.
			string originFileHash = getHash(originFile);
			DirectoryInfo recordsBaseDirectory = getDatabaseLocationForHash(originFileHash);

			// Determine if there is an existing database record for a file that can be used as a hard link for the origin file.
			bestHardLinkTarget = getAvailableHardLinkTarget(originFile, recordsBaseDirectory, maxHardLinksPerFile, maxDataAgeForNewHardLink_Days);
			
			// Determine where the new database record file would be placed to record a backup of the origin file,
			// and what that record file should be named so as to be unique.
			newRecordPath = bestHardLinkTarget.databaseRecordGroup?.FullName ?? getNextUnusedDatabaseRecordPath(recordsBaseDirectory, currentBackupTimestampString);
			newRecordFileName = getNextUnusedDatabaseRecordFileName(recordsBaseDirectory, currentBackupTimestampString);

			return new DatabaseQueryResults(newRecordPath, newRecordFileName, bestHardLinkTarget.targetFile);
		} // end getDatabaseInfoForFile()


		private (FileInfo targetFile, DirectoryInfo databaseRecordGroup)
			getAvailableHardLinkTarget(FileInfo originFile, DirectoryInfo recordsBaseDirectory, int? maxHardLinksPerFile, int? maxDataAgeForNewHardLink_Days)
		{
			FileInfo bestHardLinkTarget = null;
			DirectoryInfo hardLinkDatabaseRecordGroup = null;

			// Only bother to search for a matching hard link target if there are any records of existing matching files to hard link from,
			// and hard links are allowed in the current configuration.
			if (recordsBaseDirectory.Exists && maxHardLinksPerFile > 0)
			{
				// Get all database record groups (each group of hard link records that correespond to a single full physical copy)
				// that were created within the maximum time window allowed (if there are any).
				// For each matching group of hard links, in order from oldest to newest, search for a group having under the max allowed number of hard links.
				// In the process, verify each recorded link is valid, and delete records for those that are not.
				// Also check whether links in each group match the file under consideration.
				// If a matching group with room for more hard links exists, return one of its existing links as the match to create another link from.
				IOrderedEnumerable<DirectoryInfo> orderedCandidateRecordGroups =
					recordsBaseDirectory
						.EnumerateDirectories(TimestampFileNameFindPattern)
						.Where(directory => (maxDataAgeForNewHardLink_Days == null || getAgeFromTimestampName(directory.Name) <= maxDataAgeForNewHardLink_Days))
						.OrderByDescending(directory => getAgeFromTimestampName(directory.Name))
						.ThenBy(directory => getCounterFromTimestampName(directory.Name));
				foreach(DirectoryInfo recordGroup in orderedCandidateRecordGroups)
				{
					List<FileInfo> linksInThisGroup = recordGroup.EnumerateFiles().ToList();
					
					int numLinksInThisGroup = linksInThisGroup.Count;

					FileInfo lastValidPotentialTarget = null;
					foreach(FileInfo linkRecord in linksInThisGroup)
					{
						FileInfo potentialTargetFile = getTargetFileFromDatabaseRecord(linkRecord);
						DatabaseRecordFileValidity oldBackupFileMatchValidity = checkOldBackupFile(potentialTargetFile, originFile.Length, originFile.LastWriteTime);
						if (oldBackupFileMatchValidity == DatabaseRecordFileValidity.ValidMatch)
						{
							// Database record points to a valid, matching previous backup file.
							// If we haven't hit the max hard links allowed per physical copy, break out and stop looking for a link source.
							lastValidPotentialTarget = potentialTargetFile;
							if (numLinksInThisGroup < maxHardLinksPerFile)
								break;
						}
						else if (oldBackupFileMatchValidity == DatabaseRecordFileValidity.Invalid)
						{
							// The file referred to by this database record no longer exists or has been modified and doesn't match its hash anymore.
							// This database record is no longer valid, so delete the record, and reduce the count of valid links in this group.
							linkRecord.Delete();
							numLinksInThisGroup--;
						}
						else
						{
							// Something about the file pointed to in this record doesn't match the origin file (e.g., last modified date).
							// Can't use this one (or others that are hard links of the same physical file) as a hard link source.
							// Stop looking at records in this group of identical hard links and move on to the next group.
							break;
						}
					} // end foreach(FileInfo linkRecord in linksInThisGroup)

					if (lastValidPotentialTarget != null && numLinksInThisGroup < maxHardLinksPerFile)
					{
						// A usable record has been found in this group, and the total number of hard links recorded in this group
						// is under the maximum allowed, so use this one and go no further.
						bestHardLinkTarget = lastValidPotentialTarget;
						hardLinkDatabaseRecordGroup = recordGroup;
						break;
					}
				} // end foreach(DirectoryInfo recordGroup in orderedCandidateRecordGroups)
			} // end if (recordsBaseDirectory.Exists && maxHardLinksPerFile > 0)
			
			return (bestHardLinkTarget, hardLinkDatabaseRecordGroup);
		} // end getAvailableHardLinkTarget()


		private string getNextUnusedDatabaseRecordPath(DirectoryInfo recordsBaseDirectory, string currentBackupTimestampString)
		{
			int newGroupNameCounter;

			if (!recordsBaseDirectory.Exists)
				newGroupNameCounter = 0;
			else
			{
				IEnumerable<DirectoryInfo> existingGroupsWithThisTimestamp =
					recordsBaseDirectory
						.EnumerateDirectories(TimestampFileNameFindPattern)
						.Where(directory => directory.Name.Substring(0, currentBackupTimestampString.Length) == currentBackupTimestampString);
				if (existingGroupsWithThisTimestamp.Any())
					newGroupNameCounter = (existingGroupsWithThisTimestamp.Max(groupDirectory => (getCounterFromTimestampName(groupDirectory.Name)) + 1));
				else
					newGroupNameCounter = 0;
			}

			string nextRecordPathRelative = currentBackupTimestampString + "." + newGroupNameCounter.ToString();
			return Path.Combine(recordsBaseDirectory.FullName, nextRecordPathRelative);
		} // end getNextUnusedDatabaseRecordPath()


		private string getNextUnusedDatabaseRecordFileName(DirectoryInfo recordsBaseDirectory, string currentBackupTimestampString)
		{
			int newRecordFileNameCounter;

			if (!recordsBaseDirectory.Exists)
				newRecordFileNameCounter = 0;
			else
			{
				IEnumerable<FileInfo> existingRecordFilesWithThisTimestamp =
					recordsBaseDirectory
						.EnumerateFiles(TimestampFileNameFindPattern, SearchOption.AllDirectories)
						.Where(file => (file.Name.Substring(0, currentBackupTimestampString.Length) == currentBackupTimestampString));
				if (existingRecordFilesWithThisTimestamp.Any())
					newRecordFileNameCounter = (existingRecordFilesWithThisTimestamp.Max(file => (getCounterFromTimestampName(file.Name)) + 1));
				else
					newRecordFileNameCounter = 0;
			}

			return currentBackupTimestampString + "." + newRecordFileNameCounter.ToString();
		} // end getNextUnusedDatabaseRecordFileName()


		public void addRecord(string backedUpFileDestinationPath, DatabaseQueryResults databaseInfoForFile)
		{
			// Get an object corresponding to the directory for the new record.
			DirectoryInfo directoryForNewRecord = new DirectoryInfo(databaseInfoForFile.newRecordFilePath);
			userInterface.report($"Adding database record at {directoryForNewRecord.FullName}", ConsoleOutput.Verbosity.DebugEvents);
			// If the directory for the new record doesn't already exist, create it.
			if (!directoryForNewRecord.Exists)
				directoryForNewRecord.Create();
			
			// Create a database record file with the same timestamp name as the overall backup,
			// and write in that file the backup destination path of the file whose backup copy is being recorded.
			userInterface.report(1, $"Writing record contents: {backedUpFileDestinationPath}", ConsoleOutput.Verbosity.DebugEvents);
			using(StreamWriter fileStream = new StreamWriter(Path.Combine(databaseInfoForFile.newRecordFilePath, databaseInfoForFile.newRecordFileName), false, DatabaseFileEncoding))
			{	fileStream.Write(backedUpFileDestinationPath);	}
		} // end addRecord()


		private int getAgeFromTimestampName(string directoryName)
		{
			Regex timestampRegEx = new Regex(TimestameFileNameFindPatternRE);
			Match regExMatch = timestampRegEx.Matches(directoryName)[0];
			int year = int.Parse(regExMatch.Groups[TimeStampREGroup_Year].Value),
				month = int.Parse(regExMatch.Groups[TimeStampREGroup_Month].Value),
				day = int.Parse(regExMatch.Groups[TimeStampREGroup_Day].Value),
				hour = int.Parse(regExMatch.Groups[TimeStampREGroup_Hour].Value),
				minute = int.Parse(regExMatch.Groups[TimeStampREGroup_Minute].Value),
				second = int.Parse(regExMatch.Groups[TimeStampREGroup_Second].Value),
				millisecond = int.Parse(regExMatch.Groups[TimeStampREGroup_Millisecond].Value);
			DateTime timestampTime = new DateTime(year, month, day, hour, minute, second, millisecond);

			int age = (int)(DateTime.Now.Subtract(timestampTime).TotalDays);

			return age;
		} // end getAgeFromTimestampName()


		private int getCounterFromTimestampName(string timeStampName)
		{
			Regex timestampRegEx = new Regex(TimestameFileNameFindPatternRE);
			Match regExMatch = timestampRegEx.Matches(timeStampName)[0];
			int counter = int.Parse(regExMatch.Groups[TimeStampREGroup_Counter].Value);

			return counter;
		} // end getCounterFromTimestampName()


		private FileInfo getTargetFileFromDatabaseRecord(FileInfo databaseRecord)
		{
			FileInfo targetFile = null;
			using(StreamReader reader = new StreamReader(databaseRecord.FullName, DatabaseFileEncoding))
			{	targetFile = new FileInfo(reader.ReadToEnd());	}

			return targetFile;
		} // end getTargetFileFromDatabaseRecord()


		private DatabaseRecordFileValidity checkOldBackupFile(FileInfo backupFile, long originFileSize, DateTime originFileLastWriteTime, string previousHash = null)
		{
			if (!backupFile.Exists)
				return DatabaseRecordFileValidity.Invalid;
			else if (backupFile.Length != originFileSize || backupFile.LastWriteTime != originFileLastWriteTime)
				return DatabaseRecordFileValidity.Nonmatch;
			else if (previousHash != null && previousHash != getHash(backupFile))
				return DatabaseRecordFileValidity.Nonmatch;
			else
				return DatabaseRecordFileValidity.ValidMatch;
		} // end checkOldBackupFile()


		private string normalizeHash(byte[] hashBytes)
		{
			return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
		} // end normalizeHash()


		private string getHash(string stringToHash)
		{
			using(HashAlgorithm hasher = getHasher())
			{	return normalizeHash(hasher.ComputeHash(DatabaseFileEncoding.GetBytes(stringToHash)));	}
		} // end getHash(string)

		private string getHash(FileInfo file)
		{
			using(HashAlgorithm hasher = getHasher())
			using(FileStream stream = file.Open(FileMode.Open, FileAccess.Read))
			{	return normalizeHash(hasher.ComputeHash(stream));	}
		} // end getHash(FileInfo)


		private HashAlgorithm getHasher()
		{
			return SHA1.Create();
		} // end getHasher()


		private DirectoryInfo getDatabaseLocationForHash(string hash)
		{
			// Build a path string from the hash, one character per subdirectory.
			int expectedPathLength = databaseDirectory.FullName.Length + hash.Length * 2;
			StringBuilder locationPath = new StringBuilder(databaseDirectory.FullName, expectedPathLength);
			for(int i = 0; i < hash.Length; i++)
				locationPath.Append(Path.DirectorySeparatorChar).Append(hash[i]);
			DirectoryInfo databaseLocation = new DirectoryInfo(locationPath.ToString());

			return databaseLocation;
		} // end getDatabaseLocationForHash()

	} // end class Database
}