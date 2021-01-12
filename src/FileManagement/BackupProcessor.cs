using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace hlback.FileManagement
{
	class BackupProcessor
	{
		private const int HashLength = 40;
		private const int DatabaseFilePathLength = (HashLength - 1) * 2 + 1;
		private readonly Encoding DatabaseFileEncoding = Encoding.UTF8;

		private readonly Configuration.SystemType systemType;
		private readonly ILinker hardLinker;


		public BackupProcessor(Configuration configuration)
		{
			systemType = configuration.systemType;
			if (systemType == Configuration.SystemType.Windows)
				hardLinker = new WindowsLinker();
			else
				hardLinker = new LinuxLinker();
		} // end BackupProcessor constructor


		public void makeEntireBackup(string sourcePath, string backupsRootPath)
		{
			DirectoryInfo backupsRootDirectory = new DirectoryInfo(backupsRootPath);

			// Check for a backups database at the destination.
			DirectoryInfo databaseDirectory = new DirectoryInfo(Path.Combine(backupsRootPath, ".hlbackdatabase"));
			if (!databaseDirectory.Exists)
				databaseDirectory.Create();

			// Create subdirectory for the new backup.
			DirectoryInfo subDirectory = createBackupTimeSubdirectory(backupsRootDirectory);
			string backupTimeString = subDirectory.Name;

			// Copy all the files.
			makeFolderTreeBackup(new DirectoryInfo(sourcePath), subDirectory, databaseDirectory, backupTimeString);
		} // end makeEntireBackup()


		private DirectoryInfo createBackupTimeSubdirectory(DirectoryInfo baseDirectory)
		{
			// Create date/time-based subdirectory.
			// In the unlikely scenario one can't be created because it already exists,
			// keep trying with a new name until there is no conflict.
			string backupDestinationSubDirectoryName;
			DirectoryInfo subDirectory = null;
			while(subDirectory == null)
			{
				backupDestinationSubDirectoryName = DateTime.Now.ToString("yyyy-MM-dd.HH-mm-ss.fff");
				subDirectory = createSubDirectory(baseDirectory, backupDestinationSubDirectoryName);
				if (subDirectory == null)
					System.Threading.Thread.Sleep(1);
			}
			return subDirectory;
		} // end createBackupTimeSubdirectory()


		private void makeFolderTreeBackup(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory, DirectoryInfo databaseDirectory, string backupTimeString)
		{
			// Recurse through subdirectories, copying each one.
			foreach (DirectoryInfo individualDirectory in sourceDirectory.EnumerateDirectories())
			{
				DirectoryInfo destinationSubDirectory = destinationDirectory.CreateSubdirectory(individualDirectory.Name);
				makeFolderTreeBackup(individualDirectory, destinationSubDirectory, databaseDirectory, backupTimeString);
			}

			// Back up the files in this directory.
			foreach (FileInfo individualFile in sourceDirectory.EnumerateFiles())
			{
				// Calculate a hash of the file to be backed up.
				string fileHash = getHash(individualFile);

				// Figure out the backup destination for the current file.
				string destinationFilePath = Path.Combine(destinationDirectory.FullName, individualFile.Name);

				// Get a DirectoryInfo object corresponding to a records location for that hash in the backups database.
				DirectoryInfo databaseRecordLocation = getDatabaseRecordLocationForHash(fileHash, databaseDirectory);


				// Make a full copy of the file if needed, but otherwise create a hard link from a previous backup
				string linkFilePath = getLinkFilePath(databaseRecordLocation);
				if (linkFilePath == null)
				{
					Console.WriteLine($"Copying {individualFile.FullName}");
					Console.WriteLine($"    to {destinationFilePath}");
					individualFile.CopyTo(destinationFilePath);
				}
				else
				{
					Console.WriteLine($"Linking {linkFilePath}");
					Console.WriteLine($"    to {destinationFilePath}");
					hardLinker.createHardLink(destinationFilePath, linkFilePath);
				}
				addDatabaseRecord(databaseRecordLocation, backupTimeString, destinationFilePath, (linkFilePath == null));
			}
		} // end makeFolderTreeBackup()


		private void addDatabaseRecord(DirectoryInfo databaseRecordLocation, string backupTimeString, string newFilePath, bool isFullCopy)
		{
			// CHANGE CODE HERE
			// WORKING HERE

			StreamWriter fileStream = null;

			bool databaseFileExistedAlready = databaseFile.Exists;
			
			try
			{
				// Create or open database file.
				if (!databaseFileExistedAlready)
					fileStream = databaseFile.CreateText();
				else
					fileStream = new StreamWriter(databaseFile.FullName, true, DatabaseFileEncoding);
				
				// Write new record, consisting of the path where the new file was copied.
				// The first record of the file and the first record after an empty line correspond to full copies.
				// Hard links are recorded as new paths following immediately on the next line after the last record.
				if (isFullCopy && databaseFileExistedAlready)
					fileStream.WriteLine((string)null);
				fileStream.WriteLine(newFilePath);
			}
			finally { fileStream?.Dispose(); }
		} // end addDatabaseRecord()


		private string getLinkFilePath(DirectoryInfo databaseRecordLocation)
		{
			// ADD CODE HERE
			//if (!databaseFile.Exists)
				return null;
		} // end getLinkFilePath()


		private DirectoryInfo getDatabaseRecordLocationForHash(string hash, DirectoryInfo databaseDirectory)
		{
			DirectoryInfo locationCrawler = databaseDirectory;
			string databaseRecordLocationPath = databaseDirectory.FullName;
			for (int i = 0; i < hash.Length; i++)
			{
				databaseRecordLocationPath = Path.Combine(databaseRecordLocationPath, hash[i].ToString());
				locationCrawler = new DirectoryInfo(databaseRecordLocationPath);
				if (!locationCrawler.Exists)
					locationCrawler.Create();
			}

			return locationCrawler;
		} // end getDatabaseLocationForHash()


		private string getHash(FileInfo file)
		{
			using(SHA1 hasher = SHA1.Create())
			using(FileStream stream = file.Open(FileMode.Open, FileAccess.Read))
			{
				return BitConverter.ToString(hasher.ComputeHash(stream)).Replace("-", "").ToLower();
			}
		} // end getHash()


		private DirectoryInfo createSubDirectory(DirectoryInfo baseDirectory, string subDirectoryName)
		{
			try { return baseDirectory.CreateSubdirectory(subDirectoryName); }
			catch(IOException)
			{
				if (baseDirectory.GetDirectories(subDirectoryName).Length > 0)
					return null; // Creation failed because the directory already existed.
				else
					throw; // Creation failed for some other reason.
			}

		} // end createSubDirectory()


		private void copyFile(string newFileName, string sourceFileName, bool asHardLink = false)
		{
			if (asHardLink)
				hardLinker.createHardLink(newFileName, sourceFileName);
			else
				File.Copy(sourceFileName, newFileName);
		} // end copyFile()

	} // end class BackupProcessor
}