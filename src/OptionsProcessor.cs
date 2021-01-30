using System;
using System.IO;
using hlback.ErrorManagement;

namespace hlback
{
	// OptionsProcessor:
	/// <summary>Static class containing command-line argument processing/parsing functionality.</summary>
	static class OptionsProcessor
	{
		// SwitchType:
		/// <summary>Defines types of command-line switches.</summary>
		private enum SwitchType
		{
			NotASwitch,
			MaxDaysBeforeNewFullFileCopy,
			MaxHardLinksPerPhysicalFile,
			UnrecognizedSwitch
		}


		// Define default values for optional arguments.
        private const int DefaultMaxHardLinksPerPhysicalFile = 5;
        private const int DefaultMaxDaysBeforeNewFullFileCopy = 5;


		// getRuntimeConfiguration():
		/// <summary>Parses all the options specified on the command-line string.</summary>
		/// <returns>A <c>Configuration</c> object containing the run-time settings for this run of the application.</returns>
		/// <param name="args">An array of all the command-line arguments to parse.</param>
		/// <exception cref="ErrorManagement.OptionsException">Thrown when needed parameters are missing or specified ones are invalid.</exception>
		public static Configuration getRuntimeConfiguration(string[] args)
		{
			// Variables for holding configuration setting values to be returned.
			int? maxDaysBeforeNewFullFileCopy = null;
			int? maxHardLinksPerFile = null;
			string backupSourcePath = null;
			string backupDestinationRootPath = null;

			// Find out what platform we are running on. This is so we can allow switches to be defined on the command line
			// with a forward slash ("/") on Windows. On Linux we'll require "-" or "--" versions of the switch, because
			// a string starting with "/" is indistinguishable from a file path.
			Configuration.SystemType systemType = Configuration.getSystemType();

			// Go through each argument and find out what it is.
			for (int i = 0; i < args.Length; i++)
			{
				// Get the current argument and identify whether it is a command line switch (e.g. "-MA") or a non-switch option.
				string currentArgument = args[i];
				SwitchType argSwitchType = identifySwitch(currentArgument, systemType);

				if (argSwitchType == SwitchType.UnrecognizedSwitch)
				{
					// Seems to be a switch, but isn't a valid one.
					throw new OptionsException($"Unrecognized switch: {currentArgument}");
				}
				else if (argSwitchType == SwitchType.NotASwitch)
				{
					// This argument is not a switch. The only non-switch options are the source and destination paths for the backup.
					// The first path specified is treated as the source, and the second as the destination.
					// Any additional non-switch options encountered are treated as an error.
					if (backupSourcePath == null)
						backupSourcePath = Path.GetFullPath(currentArgument);
					else if (backupDestinationRootPath == null)
						backupDestinationRootPath = Path.GetFullPath(currentArgument);
					else
						throw new OptionsException($"Source {backupSourcePath} and Destination {backupDestinationRootPath} already specified and additional, unexpected option specified: {currentArgument}");
				}
				else
				{
					// Argument type is a recognized switch.
					// Get the next argument, which provides the value for the option specified.
					i++;
					if (i >= args.Length || identifySwitch(args[i], systemType) != SwitchType.NotASwitch)
					{
						// There is no next argument. The switch needs a value specified, and there isn't one. Error.
						throw new OptionsException($"Missing option value for switch {currentArgument}");
					}
					string optionValue = args[i];

					if (argSwitchType == SwitchType.MaxDaysBeforeNewFullFileCopy)
					{
						if (maxDaysBeforeNewFullFileCopy == null)
							maxDaysBeforeNewFullFileCopy = int.Parse(optionValue);
						else
							throw new OptionsException($"Switch -MA / --MaxHardLinkAge specified more than once"); // Same switch appeared more than once on the command line.
					}
					else if(argSwitchType == SwitchType.MaxHardLinksPerPhysicalFile)
					{
						if (maxHardLinksPerFile == null)
							maxHardLinksPerFile = int.Parse(optionValue);
						else
							throw new OptionsException($"Switch -ML / --MaxHhardLinksPerFile specified more than once"); // Same switch appeared more than once on the command line.
					}
				} // end if/else if/else block on argSwitchType
			} // end for (int i = 0; i < args.Length; i++)

			// If, when processing all the arguments, we never encountered a source path or destination path, it is an error.
			if (backupSourcePath == null)
				throw new OptionsException($"No backup source path specified");
			if (backupDestinationRootPath == null)
				throw new OptionsException($"No backup destination path specified");
			
			// Assemble a Configuration object based on the specified options.
			// For optional arguments, if they are still null (and thus weren't specified), use the default values.
			Configuration config =
				new Configuration(
					maxHardLinksPerFile ?? DefaultMaxHardLinksPerPhysicalFile,
					maxDaysBeforeNewFullFileCopy ?? DefaultMaxDaysBeforeNewFullFileCopy,
					backupSourcePath,
					backupDestinationRootPath);

			return config;
		} // end getRuntimeConfiguration()


		// identifySwitch():
		/// <summary>Identifies which option switch, if any, the specified string corresponds to.</summary>
		/// <returns>
		/// An <c>OptionsProcessor.SwitchType</c> value corresponding to the switch indicated by the specified string
		/// (which may be <c>OptionsProcessor.SwitchType.NotASwitch</c> or <c>OptionsProcessor.SwitchType.UnrecognizedSwitch</c>
		/// if the string does not appear to be a switch at all, or does not match a valid one).
		/// </returns>
		/// <param name="argument">A <c>string</c> containing a command-line argument, to try to identify as indicating a command-line switch.</param>
		/// <param name="systemType">A <c>Configuration.SystemType</c> value indicating the operating system platform currently in use.</param>
		private static SwitchType identifySwitch(string argument, Configuration.SystemType systemType)
		{
			SwitchType type; // Will contain the switch type ascertained by this function.
			
			// On Windows, allow a forward slash (/) to mark a command-line switch, which is common on that platform.
			// On Linux, we won't allow that since it is indistinguishable from the start of a path string.
			bool allowSlashSwitch = (systemType == Configuration.SystemType.Windows);

			if (argument == null)
				type = SwitchType.NotASwitch; // Obviously a null string isn't a valid switch at all.
			else
			{
				if (allowSlashSwitch && argument.Length > 1 && argument[0] == '/')
				{
					// String starts with a forward slash, and has at least one character following the slash, and forward-slash switches are allowed,
					// so grab the rest of the string after the slash and try to parse it as a switch name.
					string switchName = argument.Substring(1);

					// First try parsing it as a long switch name (e.g., "MaxHardLinkAge").
					// If it isn't recognized as that, try parsing it as a short switch name (e.g., "MA").
					type = parseSwitchText_long(switchName);
					if (type == SwitchType.NotASwitch)
						type = parseSwitchText_short(switchName);
				}
				else if (argument.Length > 2 && argument.Substring(0, 2) == "--")
				{
					// String starts with "--" switch marker and has at least one character following that.
					// "--" indicates a long switch name (e.g., "MaxHardLinkAge"), so parse it accordingly.
					type = parseSwitchText_long(argument.Substring(2));
				}
				else if (argument.Length > 1 && argument[0] == '-')
				{
					// String starts with "-" switch marker and has at least one character following that.
					// "-" indicates a short switch name (e.g., "MA"), so parse it accordingly.
					type = parseSwitchText_short(argument.Substring(1));
				}
				else
					type = SwitchType.NotASwitch; // String doesn't fit any pattern that indicates a switch.
			}

			return type;
		} // end identifySwitch()

		
		// parseSwitchText_short():
		/// <summary>
		/// Parses and identifies what command-line option switch is specified by the specified string.
		/// Tries to match the string to the short versions of switch names (e.g., "MA").
		/// </summary>
		/// <returns>An <c>OptionsProcessor.SwitchType</c> value indicating a switch type (or OptionsProcessor.SwitchType.UnrecognizedSwitch).</returns>
		/// <param name="switchName">A <c>string</c> to parse as the name of a switch.</param>
		private static SwitchType parseSwitchText_short(string switchName)
		{
			switchName = switchName.ToUpper();
			if (switchName == "MA")
				return SwitchType.MaxDaysBeforeNewFullFileCopy;
			else if (switchName == "ML")
				return SwitchType.MaxHardLinksPerPhysicalFile;
			else
				return SwitchType.UnrecognizedSwitch;
		} // end parseSwitchText_short()


		// parseSwitchText_long():
		/// <summary>
		/// Parses and identifies what command-line option switch is specified by the specified string.
		/// Tries to match the string to the short versions of switch names (e.g., "MaxHardLinkAge").
		/// </summary>
		/// <returns>An <c>OptionsProcessor.SwitchType</c> value indicating a switch type (or OptionsProcessor.SwitchType.UnrecognizedSwitch).</returns>
		/// <param name="switchName">A <c>string</c> to parse as the name of a switch.</param>
		private static SwitchType parseSwitchText_long(string switchName)
		{
			switchName = switchName.ToUpper();
			if (switchName == "MAXHARDLINKAGE")
				return SwitchType.MaxDaysBeforeNewFullFileCopy;
			else if (switchName == "MAXHARDLINKSPERFILE")
				return SwitchType.MaxHardLinksPerPhysicalFile;
			else
				return SwitchType.UnrecognizedSwitch;
		} // end parseSwitchText_long()

	} // end class OptionsProcessor
}