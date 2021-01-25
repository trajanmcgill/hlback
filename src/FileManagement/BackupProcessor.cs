using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace hlback.FileManagement
{
	class BackupProcessor
	{
		private const string TimestampDirectoryCreationPattern = "yyyy-MM-dd.HH-mm-ss.fff";

		private readonly Configuration.SystemType systemType;
		private readonly ILinker hardLinker;
		private readonly int? maxHardLinksPerFile;
		private readonly int? maxDaysBeforeNewFullFileCopy;
		private readonly string sourceRootPath, backupsDestinationRootPath;
		private readonly ConsoleOutput userInterface;


		public BackupProcessor(Configuration configuration, ConsoleOutput userInterface)
		{
			systemType = configuration.systemType;
			if (systemType == Configuration.SystemType.Windows)
				hardLinker = new WindowsLinker();
			else
				hardLinker = new LinuxLinker();
			maxHardLinksPerFile = configuration.MaxHardLinksPerFile;
			maxDaysBeforeNewFullFileCopy = configuration.MaxDaysBeforeNewFullFileCopy;
			sourceRootPath = configuration.BackupSourcePath;
			backupsDestinationRootPath = configuration.BackupDestinationPath;
			this.userInterface = userInterface;
		} // end BackupProcessor constructor


		public void doBackup()
		{
			DirectoryInfo sourceDirectory = new DirectoryInfo(sourceRootPath);

			userInterface.report($"Backing up {sourceRootPath}. Scanning directory tree...", ConsoleOutput.Verbosity.NormalEvents);

			BackupSizeInfo sourceTreeSizeInfo = scanDirectoryTree(sourceDirectory);
			userInterface.report(1, $"Total files found: {sourceTreeSizeInfo.fileCount_All}; Total Bytes: {sourceTreeSizeInfo.byteCount_All}", ConsoleOutput.Verbosity.NormalEvents);

			// Get the backups root directory and get or create the backups database at that location.
			DirectoryInfo backupsRootDirectory = new DirectoryInfo(backupsDestinationRootPath);
			Database database = new Database(backupsDestinationRootPath, userInterface);

			// Create subdirectory for this new backup.
			DirectoryInfo destinationBaseDirectory = createBackupTimeSubdirectory(backupsRootDirectory);
			string backupTimeString = destinationBaseDirectory.Name;
			userInterface.report($"Backing up to {destinationBaseDirectory.FullName}", ConsoleOutput.Verbosity.NormalEvents);

			// Copy all the files.
			DateTime copyStartTime = DateTime.Now;
			BackupSizeInfo emptyBackupSizeInfo = new BackupSizeInfo { fileCount_All = 0, fileCount_Unique = 0, byteCount_All = 0, byteCount_Unique = 0 };
			BackupSizeInfo completedBackupSizeInfo = makeFolderTreeBackup(new DirectoryInfo(sourceRootPath), destinationBaseDirectory, database, backupTimeString, sourceTreeSizeInfo, emptyBackupSizeInfo);
			DateTime copyEndTime = DateTime.Now;

			
			int totalTime = (int)Math.Round(copyEndTime.Subtract(copyStartTime).TotalSeconds);
			long totalFiles = completedBackupSizeInfo.fileCount_All,
				copiedFiles = completedBackupSizeInfo.fileCount_Unique,
				linkedFiles = totalFiles - copiedFiles;
			long totalBytes = completedBackupSizeInfo.byteCount_All,
				copiedBytes = completedBackupSizeInfo.byteCount_Unique,
				linkedBytes = totalBytes - copiedBytes;

			userInterface.report($"Backup complete.", ConsoleOutput.Verbosity.NormalEvents);
			userInterface.report(1, $"Copy process duration: {totalTime} seconds.", ConsoleOutput.Verbosity.NormalEvents);
			userInterface.report(1, $"Total files: {totalFiles} ({copiedFiles} new physical copies needed, {linkedFiles} hardlinks utilized)", ConsoleOutput.Verbosity.NormalEvents);
			userInterface.report(1, $"Total bytes: {totalBytes} ({copiedBytes} physically copied, {linkedBytes} hardlinked)", ConsoleOutput.Verbosity.NormalEvents);
		} // end makeEntireBackup()


		private BackupSizeInfo scanDirectoryTree(DirectoryInfo directory)
		{
			long fileCount = 0, byteCount = 0;


			foreach (DirectoryInfo subDirectory in directory.EnumerateDirectories())
			{
				BackupSizeInfo subDirectorySizeInfo = scanDirectoryTree(subDirectory);
				fileCount += subDirectorySizeInfo.fileCount_All;
				byteCount += subDirectorySizeInfo.byteCount_All;
			}

			foreach (FileInfo file in directory.EnumerateFiles())
			{
				fileCount++;
				byteCount += file.Length;
			}

			BackupSizeInfo treeSizeInfo =
				new BackupSizeInfo { fileCount_All = fileCount, fileCount_Unique = fileCount, byteCount_All = byteCount, byteCount_Unique = byteCount };

			return treeSizeInfo;
		} // end scanDirectoryTree()


		private DirectoryInfo createBackupTimeSubdirectory(DirectoryInfo baseDirectory)
		{
			// Create date/time-based subdirectory.
			// In the unlikely scenario one can't be created because it already exists,
			// keep trying with a new name until there is no conflict.
			string backupDestinationSubDirectoryName;
			DirectoryInfo subDirectory = null;
			while(subDirectory == null)
			{
				backupDestinationSubDirectoryName = DateTime.Now.ToString(TimestampDirectoryCreationPattern);
				subDirectory = createSubDirectory(baseDirectory, backupDestinationSubDirectoryName);
				if (subDirectory == null)
					System.Threading.Thread.Sleep(1);
			}
			return subDirectory;
		} // end createBackupTimeSubdirectory()

		
		private int getCompletionPercentage(long totalBytes, long completedBytes)
		{
			return (int)Math.Floor(100 * (decimal)completedBytes / (decimal)totalBytes);
		} // end completionPercentage()


		private BackupSizeInfo makeFolderTreeBackup(
			DirectoryInfo sourceDirectory, DirectoryInfo destinationCurrentDirectory,
			Database database, string backupTimestampString, BackupSizeInfo totalExpectedBackupSize, BackupSizeInfo previouslyCompleteSizeInfo)
		{
			BackupSizeInfo thisTreeCompletedSizeInfo = new BackupSizeInfo() { fileCount_All = 0, fileCount_Unique = 0, byteCount_All = 0, byteCount_Unique = 0 };
			int previousPercentComplete,
				percentComplete = getCompletionPercentage(totalExpectedBackupSize.byteCount_All, previouslyCompleteSizeInfo.byteCount_All);
			
			// Back up the files in this directory.
			userInterface.report($"Backing up directory {sourceDirectory.FullName}", ConsoleOutput.Verbosity.LowImportanceEvents);
			foreach (FileInfo individualFile in sourceDirectory.EnumerateFiles())
			{
				// Figure out the backup destination for the current file.
				string destinationFilePath = Path.Combine(destinationCurrentDirectory.FullName, individualFile.Name);

				// Look in the database and find an existing, previously backed up file to create a hard link to,
				// if any exists within the current run's rules for using links.
				DatabaseQueryResults databaseInfoForFile = database.getDatabaseInfoForFile(individualFile, maxHardLinksPerFile, maxDaysBeforeNewFullFileCopy, backupTimestampString);

				// Make a full copy of the file if needed, but otherwise create a hard link from a previous backup
				if (databaseInfoForFile.bestHardLinkTarget == null)
				{
					userInterface.report(1, $"Backing up file {individualFile.Name} to {destinationFilePath} [copying]", ConsoleOutput.Verbosity.LowImportanceEvents);
					individualFile.CopyTo(destinationFilePath);
					thisTreeCompletedSizeInfo.fileCount_Unique++;
					thisTreeCompletedSizeInfo.byteCount_Unique += individualFile.Length;
				}
				else
				{
					string linkFilePath = databaseInfoForFile.bestHardLinkTarget.FullName;
					userInterface.report(1, $"Backing up file {individualFile.Name} to {destinationFilePath} [identical existing file found; creating hardlink to {linkFilePath}]", ConsoleOutput.Verbosity.LowImportanceEvents);
					hardLinker.createHardLink(destinationFilePath, linkFilePath);
				}
				thisTreeCompletedSizeInfo.fileCount_All++;
				thisTreeCompletedSizeInfo.byteCount_All += individualFile.Length;
				
				previousPercentComplete = percentComplete;
				percentComplete = getCompletionPercentage(totalExpectedBackupSize.byteCount_All, previouslyCompleteSizeInfo.byteCount_All + thisTreeCompletedSizeInfo.byteCount_All);
				userInterface.reportProgress(percentComplete, previousPercentComplete, ConsoleOutput.Verbosity.NormalEvents);

				// Record in the backups database the new copy or link that was made.
				database.addRecord(destinationFilePath, databaseInfoForFile);
			}

			// Recurse through subdirectories, copying each one.
			foreach (DirectoryInfo individualDirectory in sourceDirectory.EnumerateDirectories())
			{
				DirectoryInfo destinationSubDirectory = destinationCurrentDirectory.CreateSubdirectory(individualDirectory.Name);

				BackupSizeInfo totalCompletedSizeInfo = previouslyCompleteSizeInfo + thisTreeCompletedSizeInfo;

				BackupSizeInfo subDirectoryBackupSizeInfo =
					makeFolderTreeBackup(individualDirectory, destinationSubDirectory, database, backupTimestampString, totalExpectedBackupSize, totalCompletedSizeInfo);
				
				thisTreeCompletedSizeInfo += subDirectoryBackupSizeInfo;
			}

			return thisTreeCompletedSizeInfo;
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