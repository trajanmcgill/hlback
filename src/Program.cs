using System;
using System.IO;
using hlback.FileManagement;

namespace hlback
{
    class Program
    {
        private const int MaxHardLinksPerFile = 5; // CHANGE CODE HERE (acquire this from user at run time)
        private const int MaxDaysBeforeNewFullFileCopy = 5; // CHANGE CODE HERE (acquire this from user at run time)

        static void Main(string[] args)
        {
			Configuration config = new Configuration(MaxHardLinksPerFile, MaxDaysBeforeNewFullFileCopy);

			string backupSourcePath = Path.Combine(Directory.GetCurrentDirectory(), "testSource"); // CHANGE CODE HERE
			string backupDestinationRootPath = Path.Combine(Directory.GetCurrentDirectory(), "testDestination"); // CHANGE CODE HERE

			BackupProcessor backupProcessor =
				new BackupProcessor(config, backupSourcePath, backupDestinationRootPath, new ConsoleOutput(ConsoleOutput.Verbosity.NormalEvents));
			
			backupProcessor.doBackup();
        } // end Main()

    } // end class Program
}
