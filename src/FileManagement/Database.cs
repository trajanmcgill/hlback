using System;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using System.Collections.Generic;

namespace hlback.FileManagement
{
	class Database
	{
		//private const int HashLength = 40;
		//private const int DatabaseFilePathLength = (HashLength - 1) * 2 + 1;
		private const string DatabaseDirectoryName = ".hlbackdatabase";
		private readonly Encoding DatabaseFileEncoding = Encoding.UTF8;
		private readonly int HashLength;

		private readonly DirectoryInfo databaseDirectory;


		public Database(string backupsRootPath)
		{
			HashLength = getHash("hashtest").Length;
			databaseDirectory = new DirectoryInfo(Path.Combine(backupsRootPath, DatabaseDirectoryName));
		} // end Database constructor


		public FileRecordMatchInfo getMatchingFileRecordInfo(FileInfo originFile, int maxHardLinksPerFile, int maxDaysBeforeNewFullFileCopy)
		{
			string originFileHash = getHash(originFile);
			FileInfo availableMatchingHardLinkTarget = null;
			int matchingFileGroup = 0;
			string databaseRecordFullPath;

			DirectoryInfo baseRecordsLocation = getDatabaseLocationForHash(originFileHash);

			// WORKING HERE
			// ADD CODE HERE
			// Need to clean out records pointing to deleted files first.
			/*
			if(baseRecordsLocation.Exists)
			{
				List<DirectoryInfo> groupDirectories =
					baseRecordsLocation
						.EnumerateDirectories()
						.OrderByDescending(
							fileGroupDirectory =>
							{
								int directoryNumber;
								if (!int.TryParse(fileGroupDirectory.Name, out directoryNumber))
									throw new ErrorManagement.DatabaseException($"Unexpected directory in backups database at {fileGroupDirectory.FullName}");
								return directoryNumber;
							})
						.ToList();
				foreach(DirectoryInfo fileGroupDirectory in groupDirectories)
				{
					List<FileInfo> fileRecords = fileGroupDirectory.EnumerateFiles().ToList();
					if (fileRecords.Count >= maxHardLinksPerFile)
						{
							matchingFileGroup = fileGroupDirectory.Name
						}

					List<FileInfo> orderedFileRecords = fileGroupDirectory.EnumerateFiles().OrderByDescending(fileRecord => fileRecord.Name).ToList();

				}
			}
			*/
			databaseRecordFullPath = Path.Combine(baseRecordsLocation.FullName, matchingFileGroup.ToString());





			FileRecordMatchInfo matchingFileRecordInfo = new FileRecordMatchInfo(originFileHash, availableMatchingHardLinkTarget, databaseRecordFullPath);

			return matchingFileRecordInfo;
		} // end getMatchingFileRecordInfo()


		public void addRecord(DirectoryInfo destinationBaseDirectory, string backedUpFilePath, FileRecordMatchInfo matchingFileRecord)
		{
			// If the backups database directory doesn't already exist, create it.
			if (!databaseDirectory.Exists)
				databaseDirectory.Create();
			
			// Get the database record location for this file. If that path doesn't already exist, create it.
			DirectoryInfo newRecordLocation = new DirectoryInfo(matchingFileRecord.databaseRecordFullPath);
			if (!newRecordLocation.Exists)
				newRecordLocation.Create();

			// Create a file with the same timestamp name as the overall backup, and write to it the
			// location of the file whose copy is being recorded here.
			using(StreamWriter fileStream = new StreamWriter(Path.Combine(newRecordLocation.FullName, destinationBaseDirectory.Name), false, DatabaseFileEncoding))
			{	fileStream.WriteLine(backedUpFilePath);	}
		} // end addRecord()


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

			return new DirectoryInfo(locationPath.ToString());
		} // end getDatabaseLocationForHash()

	} // end class Database
}