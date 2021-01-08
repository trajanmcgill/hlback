using System;
using System.IO;

namespace hlback.FileManagement
{
	class BackupProcessor
	{
		private readonly Configuration.SystemType systemType;
		private readonly ILinker linker;

		public BackupProcessor(Configuration configuration)
		{
			systemType = configuration.systemType;
			if (systemType == Configuration.SystemType.Windows)
				linker = new WindowsLinker();
			else
				linker = new LinuxLinker();
		}

		public void copyFile(string newFileName, string sourceFileName, bool asHardLink = false)
		{
			if (asHardLink)
				linker.createHardLink(newFileName, sourceFileName);
			else
				File.Copy(sourceFileName, newFileName);
		}
	} // end class BackupProcessor
}