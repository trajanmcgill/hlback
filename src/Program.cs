using System;
using System.IO;
using hlback.FileManagement;

namespace hlback
{
    class Program
    {
		private enum ExitCode : int
		{
			Success = 0,
			Error_General = 1,
			Error_Usage = 64
		}


        static int Main(string[] args)
        {
			Configuration config;
			ConsoleOutput userInterface = new ConsoleOutput(ConsoleOutput.Verbosity.NormalEvents);

			try { config = OptionsProcessor.getRuntimeConfiguration(args); }
			catch (ErrorManagement.OptionsException e)
			{
				userInterface.report($"Error: {e.Message}", ConsoleOutput.Verbosity.ErrorsAndWarnings);
				printUsage(userInterface);
				return (int)ExitCode.Error_Usage;
			}

			BackupProcessor backupProcessor = new BackupProcessor(config, userInterface);
			
			backupProcessor.doBackup();
			
			return (int)ExitCode.Success;
        } // end Main()


		private static void printUsage(ConsoleOutput userInterface)
		{
			userInterface.report("Usage: ADD USAGE DESCRIPTION", ConsoleOutput.Verbosity.ErrorsAndWarnings); // CHANGE CODE HERE
		} // end printUsage()

    } // end class Program
}
