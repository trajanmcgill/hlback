using System;
using System.Diagnostics;

namespace hlback.FileManagement
{
	class LinuxLinker : ILinker
	{
		public void createHardLink(string newLinkFileName, string existingTargetFileName)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo()
			{
				FileName = "/bin/cp",
				ArgumentList = {existingTargetFileName, newLinkFileName},
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using(Process commandProcess = new Process() { StartInfo = startInfo })
			{
				commandProcess.Start();
				commandProcess.WaitForExit();
			}
		} // end createHardLink()
 
	} // end class LinuxLinker
}
