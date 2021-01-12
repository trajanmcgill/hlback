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
			string recordFileName = backupTimeString + (isFullCopy ? ".full" : ".link");
			using(StreamWriter fileStream = new StreamWriter(Path.Combine(databaseRecordLocation.FullName, recordFileName), false, DatabaseFileEncoding))
			{	fileStream.WriteLine(newFilePath);	}
		} // end addDatabaseRecord()


		private string getLinkFilePath(DirectoryInfo databaseRecordLocation)
		{
			// remember to check max links per file is greater than 0 before doing any searching for link sources.

			// Each record of a previously backed-up file with a hash identical to this one is represented in the database directory
			// by a file with a timestamp name and a .full or .link extension.
			// Get all the matching records, put them in newest-to-oldest order, and then:
			// 1) Grab the newest one, with the expectation it refers to a file we can use to make a hardlink from.
			// 2) Iterate down the list until finding the ...checking that max links per file isn't exceeded, and if so do full copy
			// ...also then take that first candidate and check it still exists and still has the right hash before returning it.
			// (if it doesn't, then delete the record corresponding to it? create a new record elsewhere pointing to it? not sure, because
			// it could be a link and could be a full copy itself)
			// and this whole process could maybe be shortcutted in some ways like checking size and modification date before re-hashing.
			IEnumerable<FileInfo> fullCopyRecords = databaseRecordLocation.EnumerateFiles("*.full");
			IEnumerable<FileInfo> hardLinkRecords = databaseRecordLocation.EnumerateFiles("*.link");
			List<FileInfo> orderedFileRecords = fullCopyRecords.Concat(hardLinkRecords).OrderByDescending(fileRecord => fileRecord.Name).ToList();

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