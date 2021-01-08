using System;
using System.Runtime.InteropServices;

namespace hlback.FileManagement
{
	class WindowsLinker : ILinker
	{
		public string longFileName(string fileName)
		{
			return "\\\\?\\" + System.IO.Path.GetFullPath(fileName);
		}

		public void createHardLink(string newLinkFileName, string existingTargetFileName)
		{
			CreateHardLink(longFileName(newLinkFileName), longFileName(existingTargetFileName), IntPtr.Zero);
		}

		[DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

	} // end class WindowsLinker
}
