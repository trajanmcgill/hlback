using System;
using System.IO;

namespace hlback
{
	static class OptionsProcessor
	{
		private enum SwitchType
		{
			NotASwitch,
			MaxHardLinksPerPhysicalFile,
			MaxDaysBeforeNewFullFileCopy
		}


        private const int DefaultMaxHardLinksPerPhysicalFile = 5; 
        private const int DefaultMaxDaysBeforeNewFullFileCopy = 5;


		public static Configuration getRuntimeConfiguration(string[] args, ConsoleOutput userInterface)
		{
			int? maxHardLinksPerFile = null;
			int? maxDaysBeforeNewFullFileCopy = null;
			string backupSourcePath = null;
			string backupDestinationRootPath = null;

			Configuration.SystemType systemType = Configuration.getSystemType();

			for (int i = 0; i < args.Length; i++)
			{
				SwitchType argSwitchType = identifySwitch(args[i], systemType);
				Console.WriteLine($"args[{i}]: {argSwitchType.ToString()}");
			}

			backupSourcePath = Path.Combine(Directory.GetCurrentDirectory(), "testSource"); // CHANGE CODE HERE
			backupDestinationRootPath = Path.Combine(Directory.GetCurrentDirectory(), "testDestination"); // CHANGE CODE HERE

			// ADD CODE HERE: print usage and exit if needed things aren't specified.

			Configuration config =
				new Configuration(
					maxHardLinksPerFile ?? DefaultMaxHardLinksPerPhysicalFile,
					maxDaysBeforeNewFullFileCopy ?? DefaultMaxDaysBeforeNewFullFileCopy,
					backupSourcePath,
					backupDestinationRootPath);

			return config;
		} // end getRuntimeConfiguration()


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
					type = identifySwitch_long(switchName);
					if (type == SwitchType.NotASwitch)
						type = identifySwitch_short(switchName);
				}
				else if (arg.Length > 2 && arg.Substring(0, 2) == "--")
					type = identifySwitch_long(arg.Substring(2));
				else if (arg.Length > 1 && arg[0] == '-')
					type = identifySwitch_short(arg.Substring(1));
				else
					type = SwitchType.NotASwitch;
			}

			return type;
		} // end identifySwitch()

		private static SwitchType identifySwitch_short(string switchName)
		{
			switchName = switchName.ToUpper();
			if (switchName == "ML")
				return SwitchType.MaxHardLinksPerPhysicalFile;
			else if (switchName == "MA")
				return SwitchType.MaxDaysBeforeNewFullFileCopy;
			else
				return SwitchType.NotASwitch;
		} // end identifySwitch_short()

		private static SwitchType identifySwitch_long(string switchName)
		{
			switchName = switchName.ToUpper();
			if (switchName == "MAXHARDLINKSPERFILE")
				return SwitchType.MaxHardLinksPerPhysicalFile;
			else if (switchName == "MAXHARDLINKAGE")
				return SwitchType.MaxDaysBeforeNewFullFileCopy;
			else
				return SwitchType.NotASwitch;
		} // end identifySwitch_long()


		private static void printUsage(ConsoleOutput userInterface)
		{
			// ADD CODE HERE
		} // end printUsage()

	} // end class OptionsProcessor
}