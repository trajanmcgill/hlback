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
				userInterface.report("", ConsoleOutput.Verbosity.ErrorsAndWarnings);
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
			userInterface.report("", ConsoleOutput.Verbosity.ErrorsAndWarnings);
			userInterface.report("Usage: hlback [--MaxHardLinkAge AGE] [--MaxHardLinksPerFile LINKSCOUNT] [--SourcesFile SOURCESFILE] [SOURCE1 [SOURCE2] ...] DESTINATION", ConsoleOutput.Verbosity.ErrorsAndWarnings);
			userInterface.report("", ConsoleOutput.Verbosity.ErrorsAndWarnings);
			userInterface.report("Backs up files from each SOURCE path to a time-stamped directory in DESTINATION path.", ConsoleOutput.Verbosity.ErrorsAndWarnings);
			userInterface.report("", ConsoleOutput.Verbosity.ErrorsAndWarnings);
			userInterface.report("Where possible, creates hard links to previous backup copies instead of new full copies, subject to the below rules:", ConsoleOutput.Verbosity.ErrorsAndWarnings);
			userInterface.report("", ConsoleOutput.Verbosity.ErrorsAndWarnings);
			userInterface.report("--SourcesFile or -SF (or, on Windows only, /SourcesFile or /SF):", ConsoleOutput.Verbosity.ErrorsAndWarnings);
			userInterface.report(1, "If specified, will read a list of sources from the file SOURCESFILE.", ConsoleOutput.Verbosity.ErrorsAndWarnings);
			userInterface.report(1, "File must contain text defining a series of one or more source paths as follows:", ConsoleOutput.Verbosity.ErrorsAndWarnings);
			userInterface.report(1, "A line declaring a path to the source file or directory to be backed up, followed (for directory sources) by", ConsoleOutput.Verbosity.ErrorsAndWarnings);
			userInterface.report(1, "Any number of lines each starting with '+' (for inclusions) or '-' (for exclusions) followed by a regular expression.", ConsoleOutput.Verbosity.ErrorsAndWarnings);
			userInterface.report(1, "Each item within the source directory and its subdirectories will be tested against each regular expression rule for inclusion or exclusion.", ConsoleOutput.Verbosity.ErrorsAndWarnings);
			userInterface.report(1, "Rules are applied in the order defined, and all rules are applied to each item within that source path.", ConsoleOutput.Verbosity.ErrorsAndWarnings);
			userInterface.report("", ConsoleOutput.Verbosity.ErrorsAndWarnings);
			userInterface.report("--MaxHardLinkAge or -MA (or, on Windows only, /MaxHardLinkAge or /MA):", ConsoleOutput.Verbosity.ErrorsAndWarnings);
			userInterface.report(1, "If specified, will limit hard links to targets that are under AGE days old (creates a new full copy if all previous copies are too old).", ConsoleOutput.Verbosity.ErrorsAndWarnings);
			userInterface.report("", ConsoleOutput.Verbosity.ErrorsAndWarnings);
			userInterface.report("--MaxHardLinksPerFile or -ML (or, on Windows only, /MaxHardLinksPerFile or /ML):", ConsoleOutput.Verbosity.ErrorsAndWarnings);
			userInterface.report(1, "If specified, will limit the number of hard links to a particular physical copy to LINKSCOUNT (creates a new full copy if this number would be exceeded).", ConsoleOutput.Verbosity.ErrorsAndWarnings);
			userInterface.report("", ConsoleOutput.Verbosity.ErrorsAndWarnings);
		} // end printUsage()

    } // end class Program
}
