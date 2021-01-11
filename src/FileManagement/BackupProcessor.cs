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
			DirectoryInfo subDirectory = createUniqueSubdirectory(backupsRootDirectory);

			// Copy all the files.
			makeFolderTreeBackup(new DirectoryInfo(sourcePath), subDirectory, databaseDirectory);
		} // end makeEntireBackup()


		private DirectoryInfo createUniqueSubdirectory(DirectoryInfo baseDirectory)
		{
			// Create date/time-based subdirectory.
			// In the unlikely scenario one can't be created because it already exists,
			// keep trying with a new name until there is no conflict.
			string backupDestinationSubDirectory;
			DirectoryInfo subDirectory = null;
			while(subDirectory == null)
			{
				backupDestinationSubDirectory = DateTime.Now.ToString("yyyy-MM-dd.HH-mm-ss.fff");
				subDirectory = createSubDirectory(baseDirectory, backupDestinationSubDirectory);
				if (subDirectory == null)
					System.Threading.Thread.Sleep(1);
			}
			return subDirectory;
		} // end createUniqueSubdirectory()


		private void makeFolderTreeBackup(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory, DirectoryInfo databaseDirectory)
		{
			// Recurse through subdirectories, copying each one.
			foreach (DirectoryInfo individualDirectory in sourceDirectory.EnumerateDirectories())
			{
				DirectoryInfo destinationSubDirectory = destinationDirectory.CreateSubdirectory(individualDirectory.Name);
				makeFolderTreeBackup(individualDirectory, destinationSubDirectory, databaseDirectory);
			}

			// Back up the files in this directory.
			foreach (FileInfo individualFile in sourceDirectory.EnumerateFiles())
			{
				// Calculate a hash of the file to be backed up.
				string fileHash = getHash(individualFile);

				// Get a FileInfo object corresponding to a records file for that hash in the backups database.
				FileInfo databaseFile = getDatabaseFileForHash(fileHash, databaseDirectory);

				// Build the backup destination for the current file.
				string destinationFilePath = Path.Combine(destinationDirectory.FullName, individualFile.Name);

				// Make a full copy of the file if needed, but otherwise create a hard link from a previous backup
				string linkFilePath = getLinkFilePath(databaseFile);
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
				addDatabaseRecord(databaseFile, destinationFilePath, (linkFilePath == null));
			}
		} // end makeFolderTreeBackup()


		private void addDatabaseRecord(FileInfo databaseFile, string newFilePath, bool isFullCopy)
		{
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


		private string getLinkFilePath(FileInfo databaseFile)
		{
			// ADD CODE HERRE
			//if (!databaseFile.Exists)
				return null;
		} // end getLinkFilePath()


		private FileInfo getDatabaseFileForHash(string hash, DirectoryInfo databaseDirectory)
		{
			int i;
			DirectoryInfo pathCrawler;
			string databaseFilePath = databaseDirectory.FullName;
			for (i = 0; i < hash.Length - 1; i++)
			{
				databaseFilePath = Path.Combine(databaseFilePath, hash[i].ToString());
				pathCrawler = new DirectoryInfo(databaseFilePath);
				if (!pathCrawler.Exists)
					pathCrawler.Create();
			}
			databaseFilePath = Path.Combine(databaseFilePath, hash[i].ToString());

			return new FileInfo(databaseFilePath);
		} // end getDatabaseFileForHash()


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