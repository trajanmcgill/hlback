using System;
using System.Collections.Generic;
using System.IO;

namespace hlback.FileManagement
{
	class FileSystemWalkLevel
	{
		public readonly string RelativePath;
		public readonly IEnumerator<FileInfo> Files;
		public readonly IEnumerator<DirectoryInfo> SubDirectories;
		public readonly bool ThisItemAllowedByRules;

		public FileSystemWalkLevel(string relativePath, IEnumerator<FileInfo> files, IEnumerator<DirectoryInfo> subDirectories, bool itemAllowedByRules)
		{
			this.RelativePath = relativePath;
			this.Files = files;
			this.SubDirectories = subDirectories;
			this.ThisItemAllowedByRules = itemAllowedByRules;
		}
	} // end class FileSystemWalkLevel
}