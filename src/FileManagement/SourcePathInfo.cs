using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using hlback.ErrorManagement;

namespace hlback.FileManagement
{
	class SourcePathInfo
	{
		public readonly string BaseItemFullPath;
		private readonly RuleSet Rules;


		public SourcePathInfo(string itemPath, RuleSet rules)
		{
			this.BaseItemFullPath = itemPath;
			this.Rules = rules;
		} // end SourcePathInfo() constructor


		public BackupSizeInfo calculateSize()
		{
			long fileCount = 0, byteCount = 0;

			foreach(BackupItemInfo file in getAllItems().Where(item => (item.Type == BackupItemInfo.ItemType.File)))
			{
				fileCount++;
				byteCount += (new FileInfo(file.FullPath)).Length;
			}
			BackupSizeInfo totalSize =
				new BackupSizeInfo { fileCount_All = fileCount, fileCount_Unique = fileCount, byteCount_All = byteCount, byteCount_Unique = byteCount };

			return totalSize;
		} // end calculateSize()


		public IEnumerable<BackupItemInfo> getAllItems()
		{
			return new FileSystemWalker(BaseItemFullPath, Rules);
		} // end getAllItems()

	} // end class SourcePathInfo
}