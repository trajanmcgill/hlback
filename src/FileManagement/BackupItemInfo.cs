using System;

namespace hlback.FileManagement
{
	class BackupItemInfo
	{
		public enum ItemType { Directory, File }
		
		public readonly ItemType Type;
		public readonly string PathFromBase;

		public BackupItemInfo(ItemType type, string pathFromBase)
		{
			this.Type = type;
			this.PathFromBase = pathFromBase;
		} // end BackupItemInfo() constructor
		
	} // end class BackupItemInfo
}