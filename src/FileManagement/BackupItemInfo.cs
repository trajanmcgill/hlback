using System;
using System.IO;

namespace hlback.FileManagement
{
	class BackupItemInfo
	{
		public enum ItemType { Directory, UnreadableDirectory, File }
		
		public readonly ItemType Type;
		public readonly string RelativePath;
		public readonly string FullPath;

		public BackupItemInfo(ItemType type, string rootPath, string relativePath)
		{
			this.Type = type;
			this.RelativePath = relativePath;
			this.FullPath = Path.Combine(rootPath, relativePath);
		} // end BackupItemInfo() constructor
		
	} // end class BackupItemInfo
}