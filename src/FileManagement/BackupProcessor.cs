using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace hlback.FileManagement
{
	class BackupProcessor
	{
		private readonly Configuration.SystemType systemType;
		private readonly ILinker linker;


		public BackupProcessor(Configuration configuration)
		{
			systemType = configuration.systemType;
			if (systemType == Configuration.SystemType.Windows)
				linker = new WindowsLinker();
			else
				linker = new LinuxLinker();
		} // end BackupProcessor constructor


		public void makeEntireBackup(string sourcePath, string backupsRootPath)
		{
			DirectoryInfo backupsRootDirectory = new DirectoryInfo(backupsRootPath);

			// Check for previous backups at the backups root path.
			IEnumerable<DirectoryInfo> previousBackupDirectories = backupsRootDirectory.EnumerateDirectories("????-??-??.??-??-??.???");

			if(!previousBackupDirectories.Any())
			{
				// No previous backups exist at this destination.
				// Do a simple, complete file copy backup from the source path to a new subdirectory of the backups root directory.

				// Create subdirectory for the new backup.
				DirectoryInfo subDirectory = createUniqueSubdirectory(backupsRootDirectory);

				// Copy all the files.
				makeFolderTreeBackup(new DirectoryInfo(sourcePath), subDirectory);
			}
			else
			{
				// At least one previous backup exists here. Use the most recent one as a point of comparison,
				// and copy only files from the source that don't exist in the most recent backup.
				// For those that do exist, create a hardlink to them instead of copying from the source.
				DirectoryInfo mostRecentBackup =
					previousBackupDirectories
						.OrderByDescending(backupDirectory => long.Parse(String.Join("", backupDirectory.Name.Split(new char[] {'-', '.'}))))
						.First();

				// WORKING HERE
				Console.WriteLine("Most recent backup resides in " + mostRecentBackup.FullName);
			}
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


		private void makeFolderTreeBackup(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory)
		{
			// Recurse through subdirectories
			foreach (DirectoryInfo individualDirectory in sourceDirectory.EnumerateDirectories())
			{
				DirectoryInfo destinationSubDirectory = destinationDirectory.CreateSubdirectory(individualDirectory.Name);
				makeFolderTreeBackup(individualDirectory, destinationSubDirectory);
			}

			// Copy the files in this directory
			foreach (FileInfo individualFile in sourceDirectory.EnumerateFiles())
				individualFile.CopyTo(Path.Combine(destinationDirectory.FullName, individualFile.Name));
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
				linker.createHardLink(newFileName, sourceFileName);
			else
				File.Copy(sourceFileName, newFileName);
		} // end copyFile()

	} // end class BackupProcessor
}