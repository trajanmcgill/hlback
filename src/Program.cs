using System;
using System.IO;
using hlback.FileManagement;

namespace hlback
{
    class Program
    {
        static void Main(string[] args)
        {
			ConsoleOutput userInterface = new ConsoleOutput(ConsoleOutput.Verbosity.NormalEvents);
			Configuration config = OptionsProcessor.getRuntimeConfiguration(args, userInterface);

			BackupProcessor backupProcessor = new BackupProcessor(config, userInterface);
			
			backupProcessor.doBackup();
        } // end Main()

    } // end class Program
}
