using System;
using System.Diagnostics;

namespace hlback.FileManagement
{
	// LinuxLinker:
	/// <summary>Class for creating file hard links on a Windows environment.</summary>
	class LinuxLinker : ILinker
	{
		// createHardLink():
		/// <summary>Creates a hard link, with the specified name, to the specified existing file. [Implements ILinker.createHardLink()]</summary>
		/// <param name="newLinkFileName">A <c>string</c> with the full name (path + name) of the new link to be created.</param>
		/// <param name="existingTargetFileName">A <c>string</c> containing the full name (path + name) of an existing file whose physical data will be linked to.</param>
		public void createHardLink(string newLinkFileName, string existingTargetFileName)
		{
			// In Linux, need to do this by kicking off the cp command.
			// Set up the info for starting an external process to run cp and pass it the appropriate arguments.
			ProcessStartInfo startInfo =
				new ProcessStartInfo()
				{
					FileName = "/bin/cp",
					ArgumentList = {existingTargetFileName, newLinkFileName},
					UseShellExecute = false,
					CreateNoWindow = true
				};

			// Create the cp command process and wait for it to complete before returning.
			using(Process commandProcess = new Process() { StartInfo = startInfo })
			{
				// CHANGE CODE HERE: deal with errors running the process to create links
				commandProcess.Start();
				commandProcess.WaitForExit();
			}
		} // end createHardLink()
 
	} // end class LinuxLinker
}
