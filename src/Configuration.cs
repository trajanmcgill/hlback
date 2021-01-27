using System;
using System.Runtime.InteropServices;

namespace hlback
{
	class Configuration
	{
		public enum SystemType { Linux, Windows };

		public readonly int? MaxHardLinksPerFile;
		public readonly int? MaxDaysBeforeNewFullFileCopy;
		public readonly string BackupSourcePath;
		public readonly string BackupDestinationPath;


		public Configuration(int? maxHardLinksPerFile, int? maxDaysBeforeNewFullFileCopy, string backupSourcePath, string backupDestinationPath)
		{
			MaxHardLinksPerFile = maxHardLinksPerFile;
			MaxDaysBeforeNewFullFileCopy = maxDaysBeforeNewFullFileCopy;
			BackupSourcePath = backupSourcePath;
			BackupDestinationPath = backupDestinationPath;
		} // end Configuration constructor


		public static SystemType getSystemType()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return SystemType.Windows;
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				return SystemType.Linux;
			else
				throw new NotImplementedException("Unsupported operating system.");
		} // end getSystemType()

	} // end class Configuration
}