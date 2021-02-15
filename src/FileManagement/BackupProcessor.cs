using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using hlback.Database;

namespace hlback.FileManagement
{
	class BackupProcessor
	{
		private const string TimestampDirectoryCreationPattern = "yyyy-MM-dd.HH-mm-ss.fff";

		private readonly Encoding HashEncoding = Encoding.UTF8;

		private readonly Configuration.SystemType systemType;
		private readonly ILinker hardLinker;
		private readonly int? maxHardLinksPerFile;
		private readonly int? maxDaysBeforeNewFullFileCopy;
		private readonly List<string> sourcePaths;
		private readonly string backupsDestinationRootPath;
		private readonly ConsoleOutput userInterface;


		public BackupProcessor(Configuration configuration, ConsoleOutput userInterface)
		{
			systemType = Configuration.getSystemType();
			
			if (systemType == Configuration.SystemType.Windows)
				hardLinker = new WindowsLinker();
			else if (systemType == Configuration.SystemType.Linux)
				hardLinker = new LinuxLinker();
			else
				throw new NotImplementedException("Unsupported operating system.");
			
			maxHardLinksPerFile = configuration.MaxHardLinksPerFile;
			maxDaysBeforeNewFullFileCopy = configuration.MaxDaysBeforeNewFullFileCopy;

			sourcePaths = configuration.BackupSourcePaths;
			backupsDestinationRootPath = configuration.BackupDestinationPath;
			
			this.userInterface = userInterface;
		} // end BackupProcessor constructor


		public void doBackup()
		{
			BackupSizeInfo totalExpectedSizeInfo = new BackupSizeInfo() { fileCount_All = 0, fileCount_Unique = 0, byteCount_All = 0, byteCount_Unique = 0 };

			userInterface.report("Scanning source directory trees:", ConsoleOutput.Verbosity.NormalEvents);
			foreach (string sourcePath in sourcePaths)
			{
				userInterface.report(1, $"Scanning {sourcePath}...", ConsoleOutput.Verbosity.NormalEvents);
				BackupSizeInfo currentTreeSizeInfo = scanDirectoryTree(new DirectoryInfo(sourcePath));

				userInterface.report(2, $"Files found: {currentTreeSizeInfo.fileCount_All}; Bytes: {currentTreeSizeInfo.byteCount_All}", ConsoleOutput.Verbosity.NormalEvents);
				totalExpectedSizeInfo += currentTreeSizeInfo;
			}

			userInterface.report(1, $"Total files found: {totalExpectedSizeInfo.fileCount_All}; Total Bytes: {totalExpectedSizeInfo.byteCount_All}", ConsoleOutput.Verbosity.NormalEvents);

			// Get the backups root directory and get or create the backups database at that location.
			DirectoryInfo backupsRootDirectory = new DirectoryInfo(backupsDestinationRootPath);
			BackupsDatabase database = new BackupsDatabase(backupsDestinationRootPath, userInterface);

			// Create subdirectory for this new backup.
			DirectoryInfo destinationBaseDirectory = createBackupTimeSubdirectory(backupsRootDirectory);
			string backupTimeString = destinationBaseDirectory.Name;
			userInterface.report($"Backing up to {destinationBaseDirectory.FullName}", ConsoleOutput.Verbosity.NormalEvents);

			// Copy all the files.
			DateTime copyStartTime = DateTime.Now;
			BackupSizeInfo completedBackupSizeInfo = new BackupSizeInfo { fileCount_All = 0, fileCount_Unique = 0, byteCount_All = 0, byteCount_Unique = 0 };
			foreach (string currentSourcePath in sourcePaths)
			{
				BackupSizeInfo currentSourceBackupSizeInfo =
					makeFolderTreeBackup(new DirectoryInfo(currentSourcePath), destinationBaseDirectory, database, backupTimeString, totalExpectedSizeInfo, completedBackupSizeInfo);
				completedBackupSizeInfo += currentSourceBackupSizeInfo;
			}
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
		} // end doBackup()


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
			BackupsDatabase database, string backupTimestampString, BackupSizeInfo totalExpectedBackupSize, BackupSizeInfo previouslyCompleteSizeInfo)
		{
			BackupSizeInfo thisTreeCompletedSizeInfo = new BackupSizeInfo() { fileCount_All = 0, fileCount_Unique = 0, byteCount_All = 0, byteCount_Unique = 0 };
			int previousPercentComplete,
				percentComplete = getCompletionPercentage(totalExpectedBackupSize.byteCount_All, previouslyCompleteSizeInfo.byteCount_All);
			
			// Back up the files in this directory.
			userInterface.report($"Backing up directory {sourceDirectory.FullName}", ConsoleOutput.Verbosity.LowImportanceEvents);
			foreach (FileInfo individualFile in sourceDirectory.EnumerateFiles())
			{
				// Figure out the source file hash and its backup destination.
				string fileHash = getHash(individualFile);
				string destinationFilePath = Path.Combine(destinationCurrentDirectory.FullName, individualFile.Name);

				// Look in the database and find an existing, previously backed up file to create a hard link to,
				// if any exists within the current run's rules for using links.
				HardLinkMatch hardLinkMatch =
					database.getAvailableHardLinkMatch(fileHash, individualFile.Length, individualFile.LastWriteTimeUtc, maxHardLinksPerFile, maxDaysBeforeNewFullFileCopy);

				// Make a full copy of the file if needed, but otherwise create a hard link from a previous backup
				if (hardLinkMatch == null)
				{
					userInterface.report(1, $"Backing up file {individualFile.Name} to {destinationFilePath} [copying]", ConsoleOutput.Verbosity.LowImportanceEvents);
					individualFile.CopyTo(destinationFilePath);
					thisTreeCompletedSizeInfo.fileCount_Unique++;
					thisTreeCompletedSizeInfo.byteCount_Unique += individualFile.Length;
				}
				else
				{
					string linkFilePath = hardLinkMatch.MatchingFilePath;
					userInterface.report(1, $"Backing up file {individualFile.Name} to {destinationFilePath} [identical existing file found; creating hardlink to {linkFilePath}]", ConsoleOutput.Verbosity.LowImportanceEvents);
					hardLinker.createHardLink(destinationFilePath, linkFilePath);
				}
				thisTreeCompletedSizeInfo.fileCount_All++;
				thisTreeCompletedSizeInfo.byteCount_All += individualFile.Length;
				
				previousPercentComplete = percentComplete;
				percentComplete = getCompletionPercentage(totalExpectedBackupSize.byteCount_All, previouslyCompleteSizeInfo.byteCount_All + thisTreeCompletedSizeInfo.byteCount_All);
				userInterface.reportProgress(percentComplete, previousPercentComplete, ConsoleOutput.Verbosity.NormalEvents);

				// Record in the backups database the new copy or link that was made.
				database.addFileBackupRecord(destinationFilePath, individualFile.Length, fileHash, individualFile.LastWriteTimeUtc, (hardLinkMatch == null ? null : hardLinkMatch.ID));
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


		private string normalizeHash(byte[] hashBytes)
		{
			return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
		} // end normalizeHash()


		private HashAlgorithm getHasher()
		{
			return SHA1.Create();
		} // end getHasher()


		private string getHash(string stringToHash)
		{
			using(HashAlgorithm hasher = getHasher())
			{	return normalizeHash(hasher.ComputeHash(HashEncoding.GetBytes(stringToHash)));	}
		} // end getHash(string)

		private string getHash(FileInfo file)
		{
			using(HashAlgorithm hasher = getHasher())
			using(FileStream stream = file.Open(FileMode.Open, FileAccess.Read))
			{	return normalizeHash(hasher.ComputeHash(stream));	}
		} // end getHash(FileInfo)

	} // end class BackupProcessor
}