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
		private const string TimestampFileNameFindPattern =     "????-??-??.??-??-??.???";
		private const string TimestameFileNameFindPatternRE =  @"^(\d\d\d\d)-(\d\d)-(\d\d)\.(\d\d)-(\d\d)-(\d\d)\.(\d\d\d)$";
		private const int TimeStampREGroup_Year = 1, TimeStampREGroup_Month = 2, TimeStampREGroup_Day = 3,
			TimeStampREGroup_Hour = 4, TimeStampREGroup_Minute = 5, TimeStampREGroup_Second = 6, TimeStampREGroup_Millisecond = 7; 

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


		public FileRecordMatchInfo getMatchingFileRecordInfo(FileInfo originFile, int? maxHardLinksPerFile, int? maxDaysBeforeNewFullFileCopy)
		{
			// Get the hash of the origin file.
			string originFileHash = getHash(originFile);
			userInterface.report($"getMatchingFileRecordInfo: searcing for existing physical copies of {originFile.FullName} [hash={originFileHash}]", ConsoleOutput.Verbosity.DebugEvents);

			// Get the base location of the database records for the current file hash.
			DirectoryInfo baseRecordsLocation = getDatabaseLocationForHash(originFileHash);

			// Default, if we don't find a matching record, is to return null for the matching FileInfo object,
			// and start out with no known path for the database record group.
			FileInfo hardLinkTarget = null;
			string databaseRecordGroupPath = null;

			// There can only be a matching file record if the database records location already exists.
			if (baseRecordsLocation.Exists)
			{
				// Get all database record groups (each group of hard link records corresponding to a single full copy)
				// that were created within the maximum time window allowed (if there are any).
				IEnumerable<DirectoryInfo> candidateRecordGroups =
					baseRecordsLocation
						.EnumerateDirectories(TimestampFileNameFindPattern)
						.Where(directory => (maxDaysBeforeNewFullFileCopy == null || getAgeFromTimestampString(directory.Name) <= maxDaysBeforeNewFullFileCopy));
				
				// For each matching group of hard links, in order from oldest to newest, search for a group having under the max allowed number of hard links.
				// In the process, verify each recorded link still exists, and delete records for those that do not.
				// If a group with room for more hard links exists, return one of the existing links as the match to create another link from.
				foreach(DirectoryInfo recordGroup in candidateRecordGroups.OrderByDescending(directory => getAgeFromTimestampString(directory.Name)))
				{
					List<FileInfo> linksInThisGroup = recordGroup.EnumerateFiles().ToList();
					int numValidLinks = linksInThisGroup.Count;
					foreach(FileInfo linkRecord in linksInThisGroup)
					{
						FileInfo targetFile = getTargetFileFromDatabaseRecord(linkRecord);
						if (!fileIsStillValid(targetFile))
						{
							linkRecord.Delete();
							numValidLinks--;
						}
						else if (maxHardLinksPerFile == null || numValidLinks < maxHardLinksPerFile)
						{
							// Record does point to a valid file that still exists, and the total number of hard links recorded in this group
							// is under the maximum allowed, so use this one and go no further.
							hardLinkTarget = targetFile;
							break;
						}
						else
							userInterface.report(1, $"Maximum hardlinks per physical copy reached. Will need to create a full copy of {originFile.FullName}.", ConsoleOutput.Verbosity.DebugEvents);
					}

					// Quit looping and looking for a link to use if one has already been found.
					if (hardLinkTarget != null)
					{
						databaseRecordGroupPath = recordGroup.FullName;
						break;
					}
				}
			} // end if (baseRecordsLocation.Exists)

			if (hardLinkTarget != null)
			{
				userInterface.report(1, $"Found existing file to link to: {hardLinkTarget.FullName}", ConsoleOutput.Verbosity.DebugEvents);
				userInterface.report(1, $"In database record group stored at: {databaseRecordGroupPath}", ConsoleOutput.Verbosity.DebugEvents);
			}
			else
				userInterface.report(1, $"No existing file found to link to in database records directory {baseRecordsLocation.FullName}", ConsoleOutput.Verbosity.DebugEvents);

			return new FileRecordMatchInfo(originFileHash, baseRecordsLocation.FullName, databaseRecordGroupPath, hardLinkTarget);
		} // end getMatchingFileRecordInfo()


		public void addRecord(DirectoryInfo destinationBaseDirectory, string fullDestinationFilePath, FileRecordMatchInfo matchingFileRecord)
		{
			// Construct the location for the record to be saved in.
			// If there is a known matching existing record group, use its path.
			// If there is not, create a new one, built from the (previously calculated) base path for a file with this hash
			// combined with the same name used for the current backup's root directory.
			DirectoryInfo directoryForNewRecord = new DirectoryInfo(
				matchingFileRecord.databaseRecordGroupPath
				??
				Path.Combine(matchingFileRecord.databaseRecordBasePath, destinationBaseDirectory.Name));

			userInterface.report($"Adding database record at {directoryForNewRecord.FullName}", ConsoleOutput.Verbosity.DebugEvents);
			userInterface.report(1, $"Record contents: {fullDestinationFilePath}", ConsoleOutput.Verbosity.DebugEvents);

			// If the directory for the new record doesn't already exist, create it.
			if (!directoryForNewRecord.Exists)
				directoryForNewRecord.Create();
			
			// Create a database record file with the same timestamp name as the overall backup,
			// and write in that file the backup destination path of the file whose backup copy is being recorded.
			using(StreamWriter fileStream = new StreamWriter(Path.Combine(directoryForNewRecord.FullName, destinationBaseDirectory.Name), false, DatabaseFileEncoding))
			{	fileStream.Write(fullDestinationFilePath);	}
		} // end addRecord()


		private int getAgeFromTimestampString(string timestampString)
		{
			Regex timestampRegEx = new Regex(TimestameFileNameFindPatternRE);
			Match regExMatch = timestampRegEx.Matches(timestampString)[0];
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
		} // end getAgeFromTimestampString()


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