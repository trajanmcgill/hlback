using System;
using System.IO;
using System.Text;
using System.Collections.ObjectModel;
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
		private readonly ReadOnlyCollection<SourcePathInfo> sourcePaths;
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
			foreach (SourcePathInfo sourcePath in sourcePaths)
			{
				userInterface.report(1, $"Scanning {sourcePath.BaseItemFullPath}...", ConsoleOutput.Verbosity.NormalEvents);
				BackupSizeInfo currentTreeSizeInfo = sourcePath.Size;

				userInterface.report(2, $"Files found: {currentTreeSizeInfo.fileCount_All}; Bytes: {currentTreeSizeInfo.byteCount_All}", ConsoleOutput.Verbosity.NormalEvents);
				totalExpectedSizeInfo += currentTreeSizeInfo;
			}

			userInterface.report(1, $"Total files found: {totalExpectedSizeInfo.fileCount_All}; Total Bytes: {totalExpectedSizeInfo.byteCount_All}", ConsoleOutput.Verbosity.NormalEvents);

			// Get the backups root directory and get or create the backups database at that location.
			DirectoryInfo backupsRootDirectory = new DirectoryInfo(backupsDestinationRootPath);
			BackupsDatabase database = new BackupsDatabase(backupsDestinationRootPath, userInterface);

			// Create subdirectory for this new backup.
			DirectoryInfo destinationBaseDirectory = createBackupTimeSubdirectory(backupsRootDirectory);
			userInterface.report($"Backing up to {destinationBaseDirectory.FullName}", ConsoleOutput.Verbosity.NormalEvents);

			// Copy all the files.
			DateTime copyStartTime = DateTime.Now;
			BackupSizeInfo completedBackupSizeInfo = new BackupSizeInfo { fileCount_All = 0, fileCount_Unique = 0, byteCount_All = 0, byteCount_Unique = 0 };
			foreach (SourcePathInfo currentSourcePath in sourcePaths)
			{
				BackupSizeInfo currentSourceBackupSizeInfo =
					makeFolderTreeBackup(currentSourcePath, destinationBaseDirectory,
					                     database, totalExpectedSizeInfo, completedBackupSizeInfo);
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
			SourcePathInfo sourcePath, DirectoryInfo destinationBaseDirectory,
			BackupsDatabase database, BackupSizeInfo totalExpectedBackupSize, BackupSizeInfo previouslyCompleteSizeInfo)
		{
			BackupSizeInfo thisTreeCompletedSizeInfo = new BackupSizeInfo() { fileCount_All = 0, fileCount_Unique = 0, byteCount_All = 0, byteCount_Unique = 0 };
			int previousPercentComplete,
				percentComplete = getCompletionPercentage(totalExpectedBackupSize.byteCount_All, previouslyCompleteSizeInfo.byteCount_All);
			
			foreach (BackupItemInfo item in sourcePath.Items)
			{
				string fullItemDestinationPath = Path.Combine(destinationBaseDirectory.FullName, item.RelativePath);
				if (item.Type == BackupItemInfo.ItemType.Directory)
					(new DirectoryInfo(fullItemDestinationPath)).Create();
				else // Item is a file.
				{
					// Figure out the source file hash.
					FileInfo currentSourceFile = new FileInfo(item.FullPath);
					string fileHash = getHash(currentSourceFile);

					// Look in the database and find an existing, previously backed up file to create a hard link to,
					// if any exists within the current run's rules for using links.
					HardLinkMatch hardLinkMatch =
						database.getAvailableHardLinkMatch(
							fileHash, currentSourceFile.Length,
							currentSourceFile.LastWriteTimeUtc, maxHardLinksPerFile, maxDaysBeforeNewFullFileCopy);

					// Ensure the destination directory for this file exists.
					(new FileInfo(fullItemDestinationPath)).Directory.Create();

					// Make a full copy of the file if needed, but otherwise create a hard link from a previous backup
					if (hardLinkMatch == null)
					{
						userInterface.report(1, $"Backing up file {item.FullPath} to {fullItemDestinationPath} [copying]", ConsoleOutput.Verbosity.LowImportanceEvents);
						currentSourceFile.CopyTo(fullItemDestinationPath);
						thisTreeCompletedSizeInfo.fileCount_Unique++;
						thisTreeCompletedSizeInfo.byteCount_Unique += currentSourceFile.Length;
					}
					else
					{
						string linkFilePath = hardLinkMatch.MatchingFilePath;
						userInterface.report(1, $"Backing up file {item.FullPath} to {fullItemDestinationPath} [identical existing file found; creating hardlink to {linkFilePath}]", ConsoleOutput.Verbosity.LowImportanceEvents);
						hardLinker.createHardLink(fullItemDestinationPath, linkFilePath);
					}
					thisTreeCompletedSizeInfo.fileCount_All++;
					thisTreeCompletedSizeInfo.byteCount_All += currentSourceFile.Length;

					// Record in the backups database the new copy or link that was made.
					database.addFileBackupRecord(fullItemDestinationPath, currentSourceFile.Length, fileHash, currentSourceFile.LastWriteTimeUtc, (hardLinkMatch == null ? null : hardLinkMatch.ID));
				} // end if/else on (item.Type == BackupItemInfo.ItemType.Directory)

				previousPercentComplete = percentComplete;
				percentComplete = getCompletionPercentage(totalExpectedBackupSize.byteCount_All, previouslyCompleteSizeInfo.byteCount_All + thisTreeCompletedSizeInfo.byteCount_All);
				userInterface.reportProgress(percentComplete, previousPercentComplete, ConsoleOutput.Verbosity.NormalEvents);
			} // end foreach (BackupItemInfo item in sourcePath.Items)

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