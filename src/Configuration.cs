using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using hlback.FileManagement;

namespace hlback
{
	class Configuration
	{
		public enum SystemType { Linux, Windows, Other };

		public readonly int? MaxHardLinksPerFile;
		public readonly int? MaxDaysBeforeNewFullFileCopy;
		public readonly ReadOnlyCollection<SourcePathInfo> BackupSourcePaths;
		public readonly string BackupDestinationPath;


		public Configuration(int? maxHardLinksPerFile, int? maxDaysBeforeNewFullFileCopy, List<SourcePathInfo> backupSourcePaths, string backupDestinationPath)
		{
			MaxHardLinksPerFile = maxHardLinksPerFile;
			MaxDaysBeforeNewFullFileCopy = maxDaysBeforeNewFullFileCopy;
			BackupSourcePaths = new ReadOnlyCollection<SourcePathInfo>(backupSourcePaths);
			BackupDestinationPath = backupDestinationPath;
		} // end Configuration constructor


		public static SystemType getSystemType()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return SystemType.Windows;
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				return SystemType.Linux;
			else
				return SystemType.Other;
		} // end getSystemType()

	} // end class Configuration
}