using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace hlback
{
	class Configuration
	{
		public enum SystemType { Linux, Windows, Other };

		public readonly int? MaxHardLinksPerFile;
		public readonly int? MaxDaysBeforeNewFullFileCopy;
		public readonly List<string> BackupSourcePaths;
		public readonly string BackupDestinationPath;


		public Configuration(int? maxHardLinksPerFile, int? maxDaysBeforeNewFullFileCopy, List<string> backupSourcePaths, string backupDestinationPath)
		{
			MaxHardLinksPerFile = maxHardLinksPerFile;
			MaxDaysBeforeNewFullFileCopy = maxDaysBeforeNewFullFileCopy;
			BackupSourcePaths = backupSourcePaths;
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