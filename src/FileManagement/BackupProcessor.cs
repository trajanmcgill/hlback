using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace hlback.FileManagement
{
	class BackupProcessor
	{
		private readonly Configuration.SystemType systemType;
		private readonly ILinker hardLinker;
		private readonly int maxHardLinksPerFile, maxDaysBeforeNewFullFileCopy;
		private readonly string sourceRootPath, backupsRootPath;

		public BackupProcessor(Configuration configuration, string sourcePath, string backupsRootPath)
		{
			systemType = configuration.systemType;
			if (systemType == Configuration.SystemType.Windows)
				hardLinker = new WindowsLinker();
			else
				hardLinker = new LinuxLinker();
			maxHardLinksPerFile = configuration.MaxHardLinksPerFile;
			maxDaysBeforeNewFullFileCopy = configuration.MaxDaysBeforeNewFullFileCopy;
			sourceRootPath = sourcePath;
			this.backupsRootPath = backupsRootPath;
		} // end BackupProcessor constructor


		public void doBackup()
		{
			// Get the backups root directory and get or create the backups database at that location.
			DirectoryInfo backupsRootDirectory = new DirectoryInfo(backupsRootPath);
			Database database = new Database(backupsRootPath);

			// Create subdirectory for this new backup.
			DirectoryInfo destinationBaseDirectory = createBackupTimeSubdirectory(backupsRootDirectory);
			string backupTimeString = destinationBaseDirectory.Name;

			// Copy all the files.
			makeFolderTreeBackup(new DirectoryInfo(sourceRootPath), destinationBaseDirectory, destinationBaseDirectory, database, backupTimeString);
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


		private void makeFolderTreeBackup(
			DirectoryInfo sourceDirectory, DirectoryInfo destinationBaseDirectory,
			DirectoryInfo destinationCurrentDirectory, Database database, string backupTimeString)
		{
			// Recurse through subdirectories, copying each one.
			foreach (DirectoryInfo individualDirectory in sourceDirectory.EnumerateDirectories())
			{
				DirectoryInfo destinationSubDirectory = destinationCurrentDirectory.CreateSubdirectory(individualDirectory.Name);
				makeFolderTreeBackup(individualDirectory, destinationBaseDirectory, destinationSubDirectory, database, backupTimeString);
			}

			// Back up the files in this directory.
			foreach (FileInfo individualFile in sourceDirectory.EnumerateFiles())
			{
				// Figure out the backup destination for the current file.
				string destinationFilePath = Path.Combine(destinationCurrentDirectory.FullName, individualFile.Name);

				// Look in the database and find an existing, previously backed up file to create a hard link to,
				// if any exists within the current run's rules for using links.
				FileRecordMatchInfo destinationFileRecordInfo = database.getMatchingFileRecordInfo(individualFile);

				// Make a full copy of the file if needed, but otherwise create a hard link from a previous backup
				if (destinationFileRecordInfo.hardLinkTarget == null)
				{
					Console.WriteLine($"Copying {individualFile.FullName}");
					Console.WriteLine($"    to {destinationFilePath}");
					individualFile.CopyTo(destinationFilePath);
				}
				else
				{
					string linkFilePath = destinationFileRecordInfo.hardLinkTarget.FullName;
					Console.WriteLine($"Linking {linkFilePath}");
					Console.WriteLine($"    to {destinationFilePath}");
					hardLinker.createHardLink(destinationFilePath, linkFilePath);
				}

				// Record in the backups database the new copy or link that was made.
				database.addRecord(destinationBaseDirectory, destinationFilePath, destinationFileRecordInfo);
			}
		} // end makeFolderTreeBackup()


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