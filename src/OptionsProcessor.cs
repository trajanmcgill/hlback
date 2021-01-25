using System;
using System.IO;

namespace hlback
{
	static class OptionsProcessor
	{
        private const int DefaultMaxHardLinksPerFile = 5; 
        private const int DefaultMaxDaysBeforeNewFullFileCopy = 5;

		public static Configuration getRuntimeConfiguration(string[] args)
		{
			int maxHardLinksPerFile = DefaultMaxHardLinksPerFile; // CHANGE CODE HERE (acquire this from user at run time)
			int maxDaysBeforeNewFullFileCopy = DefaultMaxDaysBeforeNewFullFileCopy; // CHANGE CODE HERE (acquire this from user at run time)
			string backupSourcePath = Path.Combine(Directory.GetCurrentDirectory(), "testSource");  // CHANGE CODE HERE (acquire this from user at run time)
			string backupDestinationRootPath = Path.Combine(Directory.GetCurrentDirectory(), "testDestination");  // CHANGE CODE HERE (acquire this from user at run time)

			return new Configuration(maxHardLinksPerFile, maxDaysBeforeNewFullFileCopy, backupSourcePath, backupDestinationRootPath);
		} // end getRuntimeConfiguration()

	} // end class OptionsProcessor
}