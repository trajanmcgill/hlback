using System;
using System.IO;
using hlback.FileManagement;

namespace hlback
{
	// Program:
	/// <summary>Overall application class for hlback.</summary>
    class Program
    {
		// ExitCode:
		/// <summary>Defines possible return values for the application and their meanings.</summary>
		private enum ExitCode : int
		{
			Success = 0,
			Error_General = 1,
			Error_Usage = 64
		}


        // Main:
		/// <summary>Entry point for application.</summary>
		/// <returns>An <c>int</c> value corresponding to a <c>Program.ExitCode</c> enum value, indicating the application's success or failure.</returns>
		/// <param name="args">An array of all the arguments specified on the command line.</param>
		static int Main(string[] args)
        {
			// Set up objects containing the configuration info for this run and the UI object for output.
			Configuration config;
			ConsoleOutput userInterface = new ConsoleOutput(ConsoleOutput.Verbosity.NormalEvents);

			// Fill in the configuration object for this run, based on reading the command-line arguments.
			try { config = OptionsProcessor.getRuntimeConfiguration(args); }
			catch (ErrorManagement.OptionsException e)
			{
				// There was an invalid option specified or an error was encountered reading from the specified sources file. Tell the user and exit with an error code.
				userInterface.report("", ConsoleOutput.Verbosity.ErrorsAndWarnings);
				userInterface.report($"Error: {e.Message}", ConsoleOutput.Verbosity.ErrorsAndWarnings);
				printUsage(userInterface);
				return (int)ExitCode.Error_Usage;
			}
			catch (OutOfMemoryException e)
			{
				// There was an invalid option specified or an error was encountered reading from the specified sources file. Tell the user and exit with an error code.
				userInterface.report("", ConsoleOutput.Verbosity.ErrorsAndWarnings);
				userInterface.report($"Out of memory error: {e.Message}", ConsoleOutput.Verbosity.ErrorsAndWarnings);
				return (int)ExitCode.Error_General;
			}

			// CHANGE CODE HERE: ADD ERROR-HANDLING for backup process itself
			// Create a BackupProcessor object, set it to use the current run-time configuration and UI object,
			// and run the backup process.
			BackupProcessor backupProcessor = new BackupProcessor(config, userInterface);
			backupProcessor.doBackup();
			
			// Job is complete; exist the application returning a success code.
			return (int)ExitCode.Success;
        } // end Main()


        // printUsage():
		/// <summary>Prints the application usage to the specified UI.</summary>
		/// <param name="userInterface">A <c>ConsoleOutput</c> object to which to send the usage instructions string.</param>
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
