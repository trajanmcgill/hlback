using System;
using hlback.FileManagement;

namespace hlback
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting.");
			Configuration config = new Configuration();
			BackupProcessor backupProcessor = new BackupProcessor(config);
			backupProcessor.makeEntireBackup("/home/trajan/Documents/development/backupTestSource", "/home/trajan/Documents/development/backupTestDestinationRoot");
        }
    }
}
