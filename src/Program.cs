using System;
using System.IO;
using hlback.FileManagement;

namespace hlback
{
    class Program
    {
        static void Main(string[] args)
        {
			Configuration config = OptionsProcessor.getRuntimeConfiguration(args);
			ConsoleOutput userInterface = new ConsoleOutput(ConsoleOutput.Verbosity.NormalEvents);

			BackupProcessor backupProcessor = new BackupProcessor(config, userInterface);
			
			backupProcessor.doBackup();
        } // end Main()

    } // end class Program
}
