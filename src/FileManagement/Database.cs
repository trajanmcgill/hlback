using System;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace hlback.FileManagement
{
	class Database
	{
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


		public DatabaseQueryResults getDatabaseInfoForFile(FileInfo originFile, string currentBackupTimestampString, int? maxHardLinksPerFile, int? maxDaysBeforeNewFullFileCopy)
		{
			FileInfo bestHardLinkTarget;
			string newRecordPath, newRecordFileName;

			// Get the hash of the origin file.
			string originFileHash = getHash(originFile);
			userInterface.report($"getMatchingFileRecordInfo: searcing for existing physical copies of {originFile.FullName} [hash={originFileHash}]", ConsoleOutput.Verbosity.DebugEvents);

			// Get the base location of the database records for the current file hash.
			DirectoryInfo baseRecordsLocation = getDatabaseLocationForHash(originFileHash);

			// Figure out what the record filename will be if a new record is created for this file.
			int newRecordFileNameCounter;
			if (!baseRecordsLocation.Exists)
				newRecordFileNameCounter = 0;
			else
			{
				IEnumerable<FileInfo> existingRecordsWithThisTimestamp =
					baseRecordsLocation
						.EnumerateFiles(TimestampFileNameFindPattern, SearchOption.AllDirectories)
						.Where(file => file.Name.Substring(0, currentBackupTimestampString.Length) == currentBackupTimestampString);
				if (existingRecordsWithThisTimestamp.Any())
					newRecordFileNameCounter = (existingRecordsWithThisTimestamp.Max(file => getCounterFromTimestampName(file.Name)) + 1);
				else
					newRecordFileNameCounter = 0;
			}
			newRecordFileName = currentBackupTimestampString + "." + newRecordFileNameCounter.ToString();

			// Figure out if there is a database record of a matching file from which we can create a hard link.
			bestHardLinkTarget = null;
			newRecordPath = null;
			if (!baseRecordsLocation.Exists)
				newRecordPath = currentBackupTimestampString + ".0";
			else
			{
				// Get all database record groups (each group of hard link records corresponding to a single full copy)
				// that were created within the maximum time window allowed (if there are any).
				IEnumerable<DirectoryInfo> candidateRecordGroups =
					baseRecordsLocation
						.EnumerateDirectories(TimestampFileNameFindPattern)
						.Where(directory => (maxDaysBeforeNewFullFileCopy == null || getAgeFromTimestampName(directory.Name) <= maxDaysBeforeNewFullFileCopy));
				
				// For each matching group of hard links, in order from oldest to newest, search for a group having under the max allowed number of hard links.
				// In the process, verify each recorded link still is valid, and delete records for those that do not.
				// If a group with room for more hard links exists, return one of the existing links as the match to create another link from.
				IOrderedEnumerable<DirectoryInfo> orderedCandidateRecordGroups =
					candidateRecordGroups
						.OrderByDescending(directory => getAgeFromTimestampName(directory.Name))
						.ThenBy(directory => getCounterFromTimestampName(directory.Name));
				foreach(DirectoryInfo recordGroup in orderedCandidateRecordGroups)
				{
					List<FileInfo> linksInThisGroup = recordGroup.EnumerateFiles().ToList();
					int numValidLinks = linksInThisGroup.Count;
					foreach(FileInfo linkRecord in linksInThisGroup)
					{
						FileInfo targetFile = getTargetFileFromDatabaseRecord(linkRecord);
						if (!fileIsStillValid(targetFile))
						{
							// The file referred to by this database record no longer exists.
							// Delete the record, and reduce the count of valid links in this group.
							linkRecord.Delete();
							numValidLinks--;
						}
						else if (maxHardLinksPerFile == null || numValidLinks < maxHardLinksPerFile)
						{
							// Record does point to a valid file that still exists, and the total number of hard links recorded in this group
							// is under the maximum allowed, so use this one and go no further.
							bestHardLinkTarget = targetFile;
							break;
						}
						else
							userInterface.report(1, $"Maximum hardlinks per physical copy reached. Will need to create a full copy of {originFile.FullName}.", ConsoleOutput.Verbosity.DebugEvents);
					}

					// Quit looping and looking for a link to use if one has already been found.
					if (bestHardLinkTarget != null)
					{
						newRecordPath = bestHardLinkTarget.Directory.FullName;
						break;
					}
				}
			} // end if (baseRecordsLocation.Exists)

			if (newRecordPath == null)
			{
				// No usable file found to create a hard link from. Backing up the file being looked up requires a new full copy, which requires a new link record group.
				// Build the name/path of that new group.
				userInterface.report(1, $"No existing file found to link to in database records directory {baseRecordsLocation.FullName}", ConsoleOutput.Verbosity.DebugEvents);
				IEnumerable<DirectoryInfo> existingGroupsWithThisTimestamp =
					baseRecordsLocation
						.EnumerateDirectories(TimestampFileNameFindPattern)
						.Where(directory => directory.Name.Substring(0, currentBackupTimestampString.Length) == currentBackupTimestampString);
				int newGroupNameCounter;
				if (existingGroupsWithThisTimestamp.Any())
					newGroupNameCounter = (existingGroupsWithThisTimestamp.Max(file => getCounterFromTimestampName(file.Name)) + 1);
				else
					newGroupNameCounter = 0;
				newRecordPath = currentBackupTimestampString + "." + newGroupNameCounter.ToString();
			}

			if (bestHardLinkTarget != null)
			{
				// A record was found of a file usable as a hard link target.
				userInterface.report(1, $"Found existing file to link to: {bestHardLinkTarget.FullName}", ConsoleOutput.Verbosity.DebugEvents);
				userInterface.report(1, $"In database record group stored at: {newRecordPath}", ConsoleOutput.Verbosity.DebugEvents);
			}

			return new DatabaseQueryResults(Path.Combine(baseRecordsLocation.FullName, newRecordPath), newRecordFileName, bestHardLinkTarget);
		} // end getDatabaseInfoForFile()


		public void addRecord(DirectoryInfo destinationBaseDirectory, string fullDestinationFilePath, DatabaseQueryResults databaseInfoForFile)
		{
			// Construct the location for the record to be saved in.
			// If there is a known matching existing record group, use its path.
			// If there is not, create a new one, built from the (previously calculated) base path for a file with this hash
			// combined with the same name used for the current backup's root directory.
			DirectoryInfo directoryForNewRecord = new DirectoryInfo(databaseInfoForFile.newRecordFilePath);

			userInterface.report($"Adding database record at {directoryForNewRecord.FullName}", ConsoleOutput.Verbosity.DebugEvents);
			userInterface.report(1, $"Record contents: {fullDestinationFilePath}", ConsoleOutput.Verbosity.DebugEvents);

			// If the directory for the new record doesn't already exist, create it.
			if (!directoryForNewRecord.Exists)
				directoryForNewRecord.Create();
			
			// Create a database record file with the same timestamp name as the overall backup,
			// and write in that file the backup destination path of the file whose backup copy is being recorded.
			using(StreamWriter fileStream = new StreamWriter(Path.Combine(databaseInfoForFile.newRecordFilePath, databaseInfoForFile.newRecordFileName), false, DatabaseFileEncoding))
			{	fileStream.Write(fullDestinationFilePath);	}
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


		private bool fileIsStillValid(FileInfo link)
		{
			// ADD CODE HERE: consider checking file size or even allowing rehashing.

			//Console.WriteLine($"Validity = {link.Exists} for file {link.FullName}");

			return link.Exists;
		} // end fileIsStillValid()


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