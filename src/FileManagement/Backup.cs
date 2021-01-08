using System;
using System.IO;

namespace hlback.FileManagement
{
	class Backup
	{
		public static void copyFile(string source, string destination, bool asHardLink = false)
		{
			if (asHardLink)
			{}
			else
				File.Copy(source, destination);
		}
	}
}