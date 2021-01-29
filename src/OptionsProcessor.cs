using System;
using System.IO;
using hlback.ErrorManagement;

namespace hlback
{
	static class OptionsProcessor
	{
		private enum SwitchType
		{
			NotASwitch,
			MaxDaysBeforeNewFullFileCopy,
			MaxHardLinksPerPhysicalFile,
			UnrecognizedSwitch
		}


        private const int DefaultMaxHardLinksPerPhysicalFile = 5; 
        private const int DefaultMaxDaysBeforeNewFullFileCopy = 5;


		public static Configuration getRuntimeConfiguration(string[] args)
		{
			int? maxDaysBeforeNewFullFileCopy = null;
			int? maxHardLinksPerFile = null;
			string backupSourcePath = null;
			string backupDestinationRootPath = null;

			Configuration.SystemType systemType = Configuration.getSystemType();

			// Go through each argument and find out what it is.
			for (int i = 0; i < args.Length; i++)
			{
				// Get the current argument and identify whether it is a command line switch (e.g. "-MA") or a non-switch option
				string currentArgument = args[i];
				SwitchType argSwitchType = identifySwitch(currentArgument, systemType);

				if (argSwitchType == SwitchType.UnrecognizedSwitch)
					throw new OptionsException($"Unrecognized switch: {currentArgument}"); // Is a switch, but not a valid one.
				else if (argSwitchType == SwitchType.NotASwitch)
				{
					// This argument is not a switch. The only non-switch options are the source and destination paths for the backup.
					// The first path specified is treated as the source, and the second as the destination.
					// Any additional non-switch options encountered are treated as an error.
					if (backupSourcePath == null)
						backupSourcePath = parsePathOption(currentArgument);
					else if (backupDestinationRootPath == null)
						backupDestinationRootPath = parsePathOption(currentArgument);
					else
						throw new OptionsException($"Source {backupSourcePath} and Destination {backupDestinationRootPath} already specified and additional, unexpected option specified: {currentArgument}");
				}
				else
				{
					// Argument type is a recognized switch.
					// Get the next argument, which provides the value for the option specified.
					i++;
					if (i >= args.Length || identifySwitch(args[i], systemType) != SwitchType.NotASwitch)
						throw new OptionsException($"Missing option value for switch {currentArgument}");
					string optionValue = args[i];

					if (argSwitchType == SwitchType.MaxDaysBeforeNewFullFileCopy)
					{
						if (maxDaysBeforeNewFullFileCopy == null)
							maxDaysBeforeNewFullFileCopy = int.Parse(optionValue);
						else
							throw new OptionsException($"Switch -MA / --MaxHardLinkAge specified more than once");
					}
					else if(argSwitchType == SwitchType.MaxHardLinksPerPhysicalFile)
					{
						if (maxHardLinksPerFile == null)
							maxHardLinksPerFile = int.Parse(optionValue);
						else
							throw new OptionsException($"Switch -ML / --MaxHhardLinksPerFile specified more than once");
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


		private static string parsePathOption(string pathOption)
		{
			return Path.Combine(Directory.GetCurrentDirectory(), pathOption); // CHANGE CODE HERE
		} // end parsePathOption()


		private static SwitchType identifySwitch(string arg, Configuration.SystemType systemType)
		{
			SwitchType type;
			bool allowSlashSwitch = (systemType == Configuration.SystemType.Windows);

			if (arg == null)
				type = SwitchType.NotASwitch;
			else
			{
				if (allowSlashSwitch && arg.Length > 1 && arg[0] == '/')
				{
					string switchName = arg.Substring(1);
					type = parseSwitchText_long(switchName);
					if (type == SwitchType.NotASwitch)
						type = parseSwitchText_short(switchName);
				}
				else if (arg.Length > 2 && arg.Substring(0, 2) == "--")
					type = parseSwitchText_long(arg.Substring(2));
				else if (arg.Length > 1 && arg[0] == '-')
					type = parseSwitchText_short(arg.Substring(1));
				else
					type = SwitchType.NotASwitch;
			}

			return type;
		} // end identifySwitch()


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