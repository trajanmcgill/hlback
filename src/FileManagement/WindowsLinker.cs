using System;
using System.Runtime.InteropServices;

namespace hlback.FileManagement
{
	// WindowsLinker:
	/// <summary>Class for creating file hard links on a Windows environment.</summary>
	class WindowsLinker : ILinker
	{
		// createHardLink():
		/// <summary>Creates a hard link, with the specified name, to the specified existing file. [Implements ILinker.createHardLink()]</summary>
		/// <param name="newLinkFileName">A <c>string</c> with the full name (path + name) of the new link to be created.</param>
		/// <param name="existingTargetFileName">A <c>string</c> containing the full name (path + name) of an existing file whose physical data will be linked to.</param>
		public void createHardLink(string newLinkFileName, string existingTargetFileName)
		{
			// Call the external function which accomplishes this, but do so with long path versions of the file names.
			// CHANGE CODE HERE: deal with errors creating links
			CreateHardLink(longFileName(newLinkFileName), longFileName(existingTargetFileName), IntPtr.Zero);
		} // end createHardLink()


		// longFileName():
		/// <summary>Generates a Windows long file name version of a file name (prepends \\?\ to signify a long path).</summary>
		/// <returns>A <c>string</c> with the full long path version of the specified file name.</returns>
		/// <param name="fileName">A <c>string</c> containing a file name.</param>
		private string longFileName(string fileName)
		{
			return "\\\\?\\" + System.IO.Path.GetFullPath(fileName);
		} // end longFileName()


		// CreateHardLink():
		/// <summary>External function which creates a hard link, with the specified name, to the specified existing file.</summary>
		/// <param name="lpFileName">A <c>string</c> with the full name (path + name) of the new link to be created.</param>
		/// <param name="lpExistingFileName">A <c>string</c> containing the full name (path + name) of an existing file whose physical data will be linked to.</param>
		/// <param name="lpSecurityAttributes">An <c>IntPtr</c> value which must be zero/null.</param>
		[DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

	} // end class WindowsLinker
}
