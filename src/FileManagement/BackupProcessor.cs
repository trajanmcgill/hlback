using System;
using System.IO;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using hlback.Database;
using hlback.ErrorManagement;

namespace hlback.FileManagement
{
	// BackupProcessor:
	/// <summary>Class which accomplishes backups.</summary>
	class BackupProcessor
	{
		#region Private Members

		/// <summary>Encoding to be used in storing file hashes.</summary>
		private readonly Encoding HashEncoding = Encoding.UTF8;

		/// <summary>Stores the current operating system environment.</summary>
		private readonly Configuration.SystemType systemType;
		
		/// <summary>Holds an object for use creating hard links.</summary>
		private readonly ILinker hardLinker;
		
		/// <summary>Stores the maximum allowed hard links per physical backup copy. If <c>null</c>, no limit is indicated.</summary>
		private readonly int? maxHardLinksPerFile;

		/// <summary>Stores the maximum allowed age, in days, for a physical backup copy that can be used as a hard link target. If <c>null</c>, no limit is indicated.</summary>
		private readonly int? maxDaysBeforeNewFullFileCopy;
		
		/// <summary>Stores a list of all the source paths to back up.</summary>
		private readonly ReadOnlyCollection<SourcePathInfo> sourcePaths;
		
		/// <summary>Stores the root path of the backup destination.</summary>
		private readonly string backupsDestinationRootPath;
		
		/// <summary>Holds an object used for output to the user.</summary>
		private readonly ConsoleOutput userInterface;

		/// <summary>Stores a list of all warnings generated during the backup process.</summary>
		private List<string> backupProcessWarnings;

		#endregion


		#region Public Methods

		// BackupProcessor constructor:
		/// <summary>Initializes a new <c>BackupProcessor</c> object to use the given configuration and user interface object.</summary>
		/// <param name="configuration">A <c>Configuration</c> object containing all the settings for this backup run.</param>
		/// <param name="userInterface">A <c>ConsoleOutput</c> object which can be used for output to the user.</param>
		public BackupProcessor(Configuration configuration, ConsoleOutput userInterface)
		{
			// Get and store the current operating system.
			systemType = Configuration.getSystemType();
			
			// Each OS has a different means of creating hard links. Set the hardLinker object to whichever type of linker is appropriate.
			if (systemType == Configuration.SystemType.Windows)
				hardLinker = new WindowsLinker();
			else if (systemType == Configuration.SystemType.Linux)
				hardLinker = new LinuxLinker();
			else
				throw new NotImplementedException("Unsupported operating system.");
			
			// Get and store the limits on use of old physical copies for creating new hard links.
			maxHardLinksPerFile = configuration.MaxHardLinksPerFile;
			maxDaysBeforeNewFullFileCopy = configuration.MaxDaysBeforeNewFullFileCopy;

			// Get and store the source and destination info for the backup.
			sourcePaths = configuration.BackupSourcePaths;
			backupsDestinationRootPath = configuration.BackupDestinationPath;
			
			// Set up an empty list to contain any warnings generated during the backup process.
			backupProcessWarnings = new List<string>();

			// Store the user output object.
			this.userInterface = userInterface;
		} // end BackupProcessor constructor


		// doBackup():
		/// <summary>Run the actual backup job.</summary>
		public void doBackup()
		{
			// Empty the list of warnings, and create a new object to track the total expected size of the backup job.
			backupProcessWarnings.Clear();
			BackupSizeInfo totalExpectedSizeInfo = new BackupSizeInfo() { fileCount_All = 0, fileCount_Unique = 0, byteCount_All = 0, byteCount_Unique = 0 };

			// Scan all the source paths to figure out the expected size of the backup job.
			userInterface.report("Scanning source directory trees:", ConsoleOutput.Verbosity.NormalEvents);
			foreach (SourcePathInfo sourcePath in sourcePaths)
			{
				// Get the expected size for this source path.
				userInterface.report(1, $"Scanning {sourcePath.BaseItemFullPath}...", ConsoleOutput.Verbosity.NormalEvents);
				BackupSizeInfo currentTreeSizeInfo = sourcePath.calculateSize();
				userInterface.report(2, $"Files found: {currentTreeSizeInfo.fileCount_All:n0}; Bytes: {currentTreeSizeInfo.byteCount_All:n0}", ConsoleOutput.Verbosity.NormalEvents);

				// Add the results to the total expected size of the entire backup job.
				totalExpectedSizeInfo += currentTreeSizeInfo;
			}

			userInterface.report(1, $"Total files found: {totalExpectedSizeInfo.fileCount_All:n0}; Total Bytes: {totalExpectedSizeInfo.byteCount_All:n0}", ConsoleOutput.Verbosity.NormalEvents);

			// Set up variables used for tracking the backup process.
			BackupSizeInfo completedBackupSizeInfo = new BackupSizeInfo { fileCount_All = 0, fileCount_Unique = 0, byteCount_All = 0, byteCount_Unique = 0 };
			DateTime copyStartTime = DateTime.Now;
			
			try
			{
				// Get the backups root directory and ensure it exists.
				DirectoryInfo backupsRootDirectory = new DirectoryInfo(backupsDestinationRootPath);
				backupsRootDirectory.Create();

				// Open or create the backups database for this backups destination.
				using(BackupsDatabase database = new BackupsDatabase(backupsDestinationRootPath, userInterface))
				{
					// Create subdirectory for this new backup job, based on the current date and time.
					DirectoryInfo destinationBaseDirectory = createBackupTimeSubdirectory(backupsRootDirectory);
					userInterface.report($"Backing up to {destinationBaseDirectory.FullName}", ConsoleOutput.Verbosity.NormalEvents);

					// Copy all the files from each of the source paths.
					foreach (SourcePathInfo currentSourcePath in sourcePaths)
					{
						BackupSizeInfo currentSourceBackupSizeInfo;
						string driveName;

						// Call makeFolderTreeBackup() to do the copying work.
						// Usually we make a copy of the base source path item within the timestamp directory
						// (e.g., if the source path is "/foo/bar/", we create a "bar/" directory within the timestamped destination), and everything goes inside that.
						// But if if the source path is a root directory, that doesn't work, because the source directory doesn't have an actual name.
						// In that case, we get the drive name (e.g. "C" on Windows), or an empty string if there is no drive name (e.g., on Linux), append "_root",
						// and use that as the destination directory name.
						if (pathIsRootDirectory(currentSourcePath.BaseItemFullPath, out driveName)) // CHANGE CODE HERE: handle exceptions
						{
							currentSourceBackupSizeInfo =
								makeFolderTreeBackup(
									currentSourcePath, Path.Combine(destinationBaseDirectory.FullName, driveName + "_root"),
									database, totalExpectedSizeInfo, completedBackupSizeInfo);
						}
						else
						{
							currentSourceBackupSizeInfo =
								makeFolderTreeBackup(
									currentSourcePath, destinationBaseDirectory.FullName,
									database, totalExpectedSizeInfo, completedBackupSizeInfo);
						}
						
						// Update the total completed backup size info with the size of this now-completed individual source path.
						completedBackupSizeInfo += currentSourceBackupSizeInfo;
					}
				} // end using(database)
			}
			catch(System.IO.PathTooLongException ex)
			{
				userInterface.report($"Error: Path too long: {ex.Message}", ConsoleOutput.Verbosity.ErrorsAndWarnings);
				userInterface.report("You may need to enable long path support for your operating system, use a file system which supports longer paths (e.g., NTFS, ext3, or ext4 rather than FAT), or create a symbolic link to the destination directory to shorten the path string.", ConsoleOutput.Verbosity.ErrorsAndWarnings);
			}

			// Grab the end time of the copy process.
			DateTime copyEndTime = DateTime.Now;

			// Calculate how long, in seconds, the copy process took.
			int totalTime = (int)Math.Round(copyEndTime.Subtract(copyStartTime).TotalSeconds);
			
			// Figure out how many files and bytes we copied in total, as physical copies, and as hard links.
			long totalFiles = completedBackupSizeInfo.fileCount_All,
				physicallyCopiedFiles = completedBackupSizeInfo.fileCount_Unique,
				skippedFiles = completedBackupSizeInfo.fileCount_Skip,
				linkedFiles = totalFiles - physicallyCopiedFiles - skippedFiles,
				allCopiedFiles = totalFiles - skippedFiles;
			long totalBytes = completedBackupSizeInfo.byteCount_All,
				physicallyCopiedBytes = completedBackupSizeInfo.byteCount_Unique,
				skippedBytes = completedBackupSizeInfo.byteCount_Skip,
				linkedBytes = totalBytes - physicallyCopiedBytes - skippedBytes,
				allCopiedBytes = totalBytes - skippedBytes;

			userInterface.report($"Backup complete.", ConsoleOutput.Verbosity.NormalEvents);

			// If there were any warnings generated (e.g., directories skipped due to access permissions), report those.
			if (backupProcessWarnings.Count > 0)
			{
				userInterface.report("", ConsoleOutput.Verbosity.NormalEvents);
				foreach (string warning in backupProcessWarnings)
					userInterface.report($"Warning: {warning}", ConsoleOutput.Verbosity.ErrorsAndWarnings);
				userInterface.report("", ConsoleOutput.Verbosity.NormalEvents);
			}

			// Report the final totals from the backup process.
			userInterface.report(1, $"Copy process duration: {totalTime:n0} seconds.", ConsoleOutput.Verbosity.NormalEvents);
			userInterface.report(1, $"Total files copied: {allCopiedFiles:n0} ({physicallyCopiedFiles:n0} new physical copies needed, {linkedFiles:n0} hardlinks utilized)", ConsoleOutput.Verbosity.NormalEvents);
			userInterface.report(1, $"Total bytes copied: {allCopiedBytes:n0} ({physicallyCopiedBytes:n0} physically copied, {linkedBytes:n0} hardlinked)", ConsoleOutput.Verbosity.NormalEvents);
			if (skippedFiles > 0)
				userInterface.report(1, $"Skipped: {skippedFiles:n} files ({skippedBytes:n} bytes)", ConsoleOutput.Verbosity.NormalEvents);
		} // end doBackup()

		#endregion


		#region Private Methods

		// pathIsRootDirectory():
		/// <summary>Ascertains whether the specified path is a root directory, and if so figures out the root path name if there is one.</summary>
		/// <returns>A <c>bool</c> indicating whether the specified path is a root directory.</returns>
		/// <param name="path">A <c>string</c> containing a path.</param>
		/// <param name="driveName">
		/// 	An <c>out</c> parameter, a <c>string</c> containing the name of the drive (an empty string if not applicable),
		/// 	or null if it is not a root directory at all.
		/// </param>
		/// <exception cref="ArgumentNullException">Thrown when the specified path is <c>null</c>.</exception>
		/// <exception cref="System.Security.SecurityException">Thrown when a security exception is encountered trying to examine the specified path.</exception>
		/// <exception cref="ArgumentException">Thrown when the specified path is an invalid string for a path.</exception>
		/// <exception cref="PathTooLongException">Thrown when the specified path is too long.</exception>
		/// <exception cref="System.Text.RegularExpressions.RegexMatchTimeoutException">(rare or impossible) Thrown when the specified path is constructed in such a way that extracting the drive name from it times out.</exception>
		private bool pathIsRootDirectory(string path, out string driveName)
		{
			bool isRoot;
			
			// The directory is a root directory if it exists and has no parent.
			DirectoryInfo directory = new DirectoryInfo(path);
			isRoot = (directory.Exists && directory.Parent == null);
			
			// If in Windows, the root path has a name, like "C:\". Get the alphanumeric portion of that to return.
			Regex nameExpr = new Regex("^[a-zA-Z0-9]");
			driveName = isRoot ? nameExpr.Match(Path.GetFullPath(path)).Value : null;

			return isRoot;
		} // end pathIsRootDirectory()


		// createBackupTimeSubdirectory():
		/// <summary>
		/// 	Creates a new directory within the specified base directory, with a name based on the current date and time.
		/// 	In the unexpected case where there is already a directory with that name, waits a millisecond and then tries again.
		/// </summary>
		/// <returns>A <c>DirectoryInfo</c> object corresponding to the newly created directory.</returns>
		/// <param name="baseDirectory">A <c>DirectoryInfo</c> object corresponding to the directory in which to create the subdirectory.</param>
		/// <exception cref="PathException">Thrown when the subdirectory cannot be created, for instance when the drive is full or the path is too long.</exception>
		private DirectoryInfo createBackupTimeSubdirectory(DirectoryInfo baseDirectory)
		{
			// Create a date/time-based subdirectory.
			// In the unlikely scenario one can't be created because it already exists,
			// keep trying with a new name until there is no conflict.
			string TimestampDirectoryCreationPattern = "yyyy-MM-dd.HH-mm-ss.fff";
			string backupDestinationSubDirectoryName;
			DirectoryInfo subDirectory = null;
			while (subDirectory == null)
			{
				backupDestinationSubDirectoryName = DateTime.Now.ToString(TimestampDirectoryCreationPattern);
				try
				{	subDirectory = createSubDirectory(baseDirectory, backupDestinationSubDirectoryName);	} // CHANGE CODE HERE: Handle all the exceptions that can happen.
				catch (ArgumentException)
				{	throw new PathException($"Error: Unable to create base backup directory \"{backupDestinationSubDirectoryName}\" because it is an invalid directory name. Ensure that the destination drive is not formatted with FAT or another file system that does not support long filenames.");	}
				catch (IOException e)
				{	throw new PathException($"Error: Unable to create base backup directory \"{backupDestinationSubDirectoryName}\". Drive full or other IO Error.", e);	}

				if (subDirectory == null)
					System.Threading.Thread.Sleep(1);
			}
			return subDirectory;
		} // end createBackupTimeSubdirectory()

		
		// getCompletionPercentage():
		/// <summary>Returns, as a percentage, the portion of the specified total byte size that is the specified completed byte size.</summary>
		/// <returns>An <c>int</c> value indicating the percentage of completion.</returns>
		/// <param name="totalBytes">A <c>long</c> value, the number of total bytes.</param>
		/// <param name="completedBytes">A <c>long</c> value, the number of completed bytes.</param>
		private int getCompletionPercentage(long totalBytes, long completedBytes)
		{
			// Watch out for divide-by-zero.
			if (totalBytes == 0)
				return 100;
			
			return (int)Math.Floor(100 * (decimal)completedBytes / (decimal)totalBytes);
		} // end completionPercentage()


		// makeFolderTreeBackup():
		/// <summary>Does the backup copying from a specified source path to a specified destination, storing info in the specified database.</summary>
		/// <returns>A <c>BackupSizeInfo</c> object containing the total size of the copy job that was completed.</returns>
		/// <param name="sourceInfo">The source to copy from.</param>
		/// <param name="destinationBasePath">The base destination path.</param>
		/// <param name="database">The database to use for looking up and storing copy and hard link match info.</param>
		/// <param name="totalExpectedBackupSize">The total expected size of the backup job that is in progress.</param>
		/// <param name="previouslyCompleteSizeInfo">The total size of the backup job completed up to this point.</param>
		private BackupSizeInfo makeFolderTreeBackup(
			SourcePathInfo sourceInfo, string destinationBasePath,
			BackupsDatabase database, BackupSizeInfo totalExpectedBackupSize, BackupSizeInfo previouslyCompleteSizeInfo)
		{
			// Set up variable to track the size of copying done within this directory tree.
			BackupSizeInfo thisTreeCompletedSizeInfo = new BackupSizeInfo() { fileCount_All = 0, fileCount_Unique = 0, byteCount_All = 0, byteCount_Unique = 0 };
			
			// Set up variables for tracking ongoing changes to the completion percentage, for the purpose of reporting updates to the user.
			int previousPercentComplete,
				percentComplete = getCompletionPercentage(totalExpectedBackupSize.byteCount_All, previouslyCompleteSizeInfo.byteCount_All);
			
			// Iterate through each item in the source, copying it to the destination.
			foreach (BackupItemInfo item in sourceInfo.getAllItems())
			{
				// Get the full path string for the object as it will exist in the destination.
				string fullItemDestinationPath = Path.Combine(destinationBasePath, item.RelativePath);
				
				if (item.Type == BackupItemInfo.ItemType.Directory)
					(new DirectoryInfo(fullItemDestinationPath)).Create(); // Item is a directory, so simply create it at the destination location.
				else if (item.Type == BackupItemInfo.ItemType.UnreadableDirectory)
					backupProcessWarnings.Add($"Directory skipped due to unauthorized access error: {item.RelativePath}"); // Item is a directory but can't be read, so skip it and add a warning to show the user at the end.
				else // Item is a file.
				{
					// Calculate the source file hash.
					FileInfo currentSourceFile = new FileInfo(item.FullPath);
					string fileHash;
					try
					{ fileHash = getHash(currentSourceFile); } // CHANGE CODE HERE: Handle all the possible exceptions
					catch (Exception ex) when (ex is UnauthorizedAccessException || ex is System.IO.IOException)
					{
						if (ex is UnauthorizedAccessException)
							backupProcessWarnings.Add($"File skipped due to unauthorized access error: {item.RelativePath}");
						else
							backupProcessWarnings.Add($"File skipped, unable to read. Another process may be using the file: {item.RelativePath}");
						thisTreeCompletedSizeInfo.fileCount_Skip++;
						thisTreeCompletedSizeInfo.fileCount_All++;
						thisTreeCompletedSizeInfo.byteCount_Skip += currentSourceFile.Length;
						thisTreeCompletedSizeInfo.byteCount_All += currentSourceFile.Length;
						previousPercentComplete = percentComplete;
						percentComplete = getCompletionPercentage(totalExpectedBackupSize.byteCount_All, previouslyCompleteSizeInfo.byteCount_All + thisTreeCompletedSizeInfo.byteCount_All);
						userInterface.reportProgress(percentComplete, previousPercentComplete, ConsoleOutput.Verbosity.NormalEvents);
						continue; // Skip to the next item in the foreach loop.
					}

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
						// No hard link match found in the database, so do a copy operation.
						userInterface.report(1, $"Backing up file {item.FullPath} to {fullItemDestinationPath} [copying]", ConsoleOutput.Verbosity.LowImportanceEvents);
						currentSourceFile.CopyTo(fullItemDestinationPath);
						thisTreeCompletedSizeInfo.fileCount_Unique++;
						thisTreeCompletedSizeInfo.byteCount_Unique += currentSourceFile.Length;
					}
					else
					{
						// A usable hard link match was found in the database, so create a hard link instead of making a new full copy.
						string linkFilePath = hardLinkMatch.MatchingFilePath;
						userInterface.report(1, $"Backing up file {item.FullPath} to {fullItemDestinationPath} [identical existing file found; creating hardlink to {linkFilePath}]", ConsoleOutput.Verbosity.LowImportanceEvents);
						hardLinker.createHardLink(fullItemDestinationPath, linkFilePath); // CHANGE CODE HERE: Handle exceptions.
					}

					// Add this file to the total copy amounts being tracked.
					thisTreeCompletedSizeInfo.fileCount_All++;
					thisTreeCompletedSizeInfo.byteCount_All += currentSourceFile.Length;

					// Record in the backups database the new copy or link that was made.
					FileInfo newFile = new FileInfo(fullItemDestinationPath);
					database.addFileBackupRecord(fullItemDestinationPath, newFile.Length, fileHash, newFile.LastWriteTimeUtc, (hardLinkMatch == null ? null : hardLinkMatch.ID));
				} // end if/else on (item.Type == BackupItemInfo.ItemType.Directory)

				// Figure out the new completion percentage, and update the user on the progress.
				previousPercentComplete = percentComplete;
				percentComplete = getCompletionPercentage(totalExpectedBackupSize.byteCount_All, previouslyCompleteSizeInfo.byteCount_All + thisTreeCompletedSizeInfo.byteCount_All);
				userInterface.reportProgress(percentComplete, previousPercentComplete, ConsoleOutput.Verbosity.NormalEvents);
			} // end foreach (BackupItemInfo item in sourcePath.Items)

			return thisTreeCompletedSizeInfo;
		} // end makeFolderTreeBackup()


		// createSubDirectory():
		/// <summary>Create a subdirectory below the specified base directory.</summary>
		/// <returns>A <c>DirectoryInfo</c> object corresponding to the newly created directory, or <c>null</c> if the directory already existed.</returns>
		/// <param name="baseDirectory">The base directory under which to create a new subdirectory.</param>
		/// <param name="subDirectoryName">The name of the subdirectory to create.</param>
		/// <exception cref="ArgumentException">Thrown when an invalid argument was specified.</exception>
		/// <exception cref="PathException">Thrown when a path error is encountered trying to complete the operation.</exception>
		/// <exception cref="NotSupportedException">Thrown when the operation is not supported by the underlying system.</exception>
		/// <exception cref="IOException">Thrown when a general IO error occurs.</exception>
		private DirectoryInfo createSubDirectory(DirectoryInfo baseDirectory, string subDirectoryName)
		{
			try
			{
				// Figure out what the full path will be of the new directory.
				string newFullPath = Path.GetFullPath(Path.Combine(baseDirectory.FullName, subDirectoryName));

				// Create the directory and return it.
				return System.IO.Directory.CreateDirectory(newFullPath);
			}
			catch (ArgumentNullException)
			{	throw new ArgumentException($"Error trying to create directory: [null] is an invalid directory name.");	}
			catch (ArgumentException)
			{	throw new ArgumentException($"Error trying to create directory: {subDirectoryName} is an invalid directory name.");	}
			catch (PathTooLongException)
			{	throw new PathException($"Error trying to create directory, path too long trying to create directory: {subDirectoryName} inside of {baseDirectory.FullName}");	}
			catch (DirectoryNotFoundException)
			{	throw new PathException($"Error, path not found trying to create subdirectory inside of directory {baseDirectory.FullName}");	}
			catch (System.Security.SecurityException e)
			{	throw new PathException($"Error trying to create directory, security exception trying to create directory: {subDirectoryName} inside of {baseDirectory.FullName}", e);	}
			catch (UnauthorizedAccessException)
			{	throw new PathException($"Error trying to create directory, unauthorized access exception trying to create directory: {subDirectoryName} inside of {baseDirectory.FullName}");	}
			catch (NotSupportedException e)
			{	throw new NotSupportedException("Error: Unexpected error trying to create directory {} inside of directory {}", e);	}
			catch (IOException)
			{
				if (baseDirectory.GetDirectories(subDirectoryName).Length > 0)
					return null; // Creation failed because the directory already existed.
				else
					throw; // Creation failed for some other reason.
			}
		} // end createSubDirectory()


		// normalizeHash():
		/// <summary>Converts a hash in byte array form to a normalized string form (all lowercase, no dashes)</summary>
		/// <returns>A <c>string</c> containing the specified hash value in normalized form (all lowercase, no dashes)</returns>
		/// <param name="hashBytes">A hash value, as an array of bytes.</param>
		/// <exception cref="ArgumentNullException">Thrown when the hashBytes argument is <c>null</c>.</exception>
		private string normalizeHash(byte[] hashBytes)
		{
			return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
		} // end normalizeHash()


		// getHasher():
		/// <summary>Obtains an object used for hashing files.</summary>
		/// <returns>A <c>HashAlgorithm</c> object.</returns>
		private HashAlgorithm getHasher()
		{
			return SHA1.Create();
		} // end getHasher()


		// getHash():
		/// <summary>Calculates the hash of the specified file.</summary>
		/// <returns>The hashed value of the specified file.</returns>
		/// <param name="file">The file from whose contents to calculate a hash value.</param>
		/// <exception cref="System.Security.SecurityException">Thrown when the encountering a security exception trying to read from the file.</exception>
		/// <exception cref="FileNotFoundException">Thrown when the file is not found.</exception>
		/// <exception cref="UnauthorizedAccessException">Thrown when the user does not have access to the file.</exception>
		/// <exception cref="DirectoryNotFoundException">Thrown when the directory is not found.</exception>
		/// <exception cref="IOException">Thrown when encountering an IO error.</exception>
		/// <exception cref="ArgumentNullException">Thrown when the <c>file</c> argument is <c>null</c>.</exception>
		private string getHash(FileInfo file)
		{
			if (file == null)
				throw new ArgumentNullException("Error: cannot calculate the hash of file [null].");
			
			using(HashAlgorithm hasher = getHasher())
			using(FileStream stream = file.Open(FileMode.Open, FileAccess.Read))
			{	return normalizeHash(hasher.ComputeHash(stream));	}
		} // end getHash(FileInfo)

		#endregion

	} // end class BackupProcessor
}