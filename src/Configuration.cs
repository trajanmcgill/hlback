using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using hlback.FileManagement;

namespace hlback
{
	// Configuration:
	/// <summary>Represents a group of settings that apply to a run of the application.</summary>
	class Configuration
	{
		// SystemType:
		/// <summary>Enum with values corresponding to possible operating system environments.</summary>
		public enum SystemType { Linux, Windows, Other };

		/// <summary>
		/// 	Value indicating the maximum number of files which should be allowed to point to the same actual stored data before making a new physical copy of a file.
		/// 	If <c>null</c>, indicates the number of hard links per file has no limit.
		/// </summary>
		public readonly int? MaxHardLinksPerFile;

		/// <summary>
		/// 	Value indicating the maximum age (in days) a previous backup copy can attain before new hard links are not made to it and a new physical copy is made instead.
		/// 	If <c>null</c>, indicates there is no such age limit.
		/// </summary>
		public readonly int? MaxDaysBeforeNewFullFileCopy;

		/// <summary>A collection of source paths from which to make backups.</summary>
		public readonly ReadOnlyCollection<SourcePathInfo> BackupSourcePaths;

		/// <summary>A full path to a base location in which backups should be placed.</summary>
		public readonly string BackupDestinationPath;


		// Configuration constructor:
		/// <summary>Sets up the new <c>Configuration</c> object.</summary>
		/// <param name="maxHardLinksPerFile">
		/// 	Nullable <c>int</c> indicating the maximum number of files which should be allowed to point to the same actual stored data before making a new physical copy of a file.
		/// 	If <c>null</c>, indicates the number of hard links per file has no limit.
		/// </param>
		/// <param name="maxDaysBeforeNewFullFileCopy">
		/// 	Nullable <c>int</c> indicating the maximum age (in days) a previous backup copy can attain before new hard links are not made to it and a new physical copy is made instead.
		/// 	If <c>null</c>, indicates there is no such age limit.
		/// </param>
		/// <param name="backupSourcePaths">A collection of source paths from which to make backups.</param>
		/// <param name="backupDestinationPath">A full path to a base location in which backups should be placed.</param>
		/// <exception cref="ArgumentNullException">Thrown when backupSourcePaths argument is null.</exception>
		public Configuration(int? maxHardLinksPerFile, int? maxDaysBeforeNewFullFileCopy, List<SourcePathInfo> backupSourcePaths, string backupDestinationPath)
		{
			if (backupSourcePaths == null)
				throw new ArgumentNullException("Error: backupSourcePaths list cannot be null.");
			
			// Initialize member variables.
			MaxHardLinksPerFile = maxHardLinksPerFile;
			MaxDaysBeforeNewFullFileCopy = maxDaysBeforeNewFullFileCopy;
			BackupSourcePaths = new ReadOnlyCollection<SourcePathInfo>(backupSourcePaths);
			BackupDestinationPath = backupDestinationPath;
		} // end Configuration constructor


		// getSystemType():
		/// <summary>Ascertains the current operating system environment.</summary>
		/// <returns>A <c>Configuration.SystemType</c> enum value corresponding to the operating system platform in use.</returns>
		public static SystemType getSystemType()
		{
			// Recognize Windows or Linux; other platforms not supported at this time.
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return SystemType.Windows;
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				return SystemType.Linux;
			else
				return SystemType.Other;
		} // end getSystemType()

	} // end class Configuration
}