using System;
using hlback.FileManagement;

namespace hlback
{
    class Program
    {
        private const int MaxHardLinksPerFile = 5; // CHANGE CODE HERE (acquire this from user at run time)
        private const int MaxDaysBeforeNewFullFileCopy = 5; // CHANGE CODE HERE (acquire this from user at run time)

        static void Main(string[] args)
        {
            Console.WriteLine("Starting.");
			Configuration config = new Configuration(MaxHardLinksPerFile, MaxDaysBeforeNewFullFileCopy);
			BackupProcessor backupProcessor = new BackupProcessor(config);
			backupProcessor.makeEntireBackup(
                "/home/trajan/Documents/development/backupTestSource",
                "/home/trajan/Documents/development/backupTestDestinationRoot");
        }
    }
}
