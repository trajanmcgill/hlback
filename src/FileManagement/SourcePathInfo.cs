using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

namespace hlback.FileManagement
{
	class SourcePathInfo
	{
		public readonly string BaseDirectoryPath;
		
		public BackupSizeInfo Size
		{
			get
			{
				if (_Size == null)
					_Size = calculateSize();
				return _Size;
			}
		} // end Size property

		public IEnumerable<BackupItemInfo> Items
		{
			get
			{
				foreach (BackupItemInfo item in getItems("", true))
					yield return item;
			}
		} // end Items property


		private BackupSizeInfo _Size = null;
		private readonly List<(bool, Regex)> Rules;


		public SourcePathInfo(string basePath, List<string> ruleDefinitions)
		{
			this.BaseDirectoryPath = basePath;
			
			Rules = new List<(bool, Regex)>();
			if (ruleDefinitions != null)
			{
				foreach (string individualRuleDefinition in ruleDefinitions)
				{
					if (individualRuleDefinition.Length < 1)
						continue;
					
					if (individualRuleDefinition[0] == '+')				
						Rules.Add((true, new Regex(individualRuleDefinition)));
					else if (individualRuleDefinition[0] == '-')
						Rules.Add((false, new Regex(individualRuleDefinition)));
				}
			}
		} // end SourcePathInfo() constructor


		private IEnumerable<BackupItemInfo> getItems(string pathFromBase, bool defaultUsability)
		{
			DirectoryInfo currentDirectory = new DirectoryInfo(Path.Combine(BaseDirectoryPath, pathFromBase));
			
			foreach (DirectoryInfo subDirectory in currentDirectory.EnumerateDirectories())
			{
				string subDirectoryPathFromBase = Path.Combine(pathFromBase, subDirectory.Name);
				bool thisSubDirectoryIncluded = itemAllowedByRules(subDirectoryPathFromBase, defaultUsability);

				if (thisSubDirectoryIncluded)
					yield return new BackupItemInfo(BackupItemInfo.ItemType.Directory, subDirectoryPathFromBase);

				foreach (BackupItemInfo item in getItems(subDirectoryPathFromBase, thisSubDirectoryIncluded))
					yield return item;
			}

			foreach (FileInfo file in currentDirectory.EnumerateFiles())
			{
				string filePathFromBase = Path.Combine(pathFromBase, file.Name);
				if (itemAllowedByRules(filePathFromBase, defaultUsability))
					yield return new BackupItemInfo(BackupItemInfo.ItemType.File, filePathFromBase);
			}
		} // end getItems()


		private bool itemAllowedByRules(string path, bool defaultUsability)
		{
			bool useThisItem = defaultUsability;

			if (Rules.Count > 0)
			{
				foreach ((bool includeMatchingItems, Regex expression) rule in Rules)
				{
					if (rule.expression.IsMatch(path))
						useThisItem = rule.includeMatchingItems;
				}
			}

			return useThisItem;
		} // end itemAllowedByRules()


		private BackupSizeInfo calculateSize()
		{
			long fileCount = 0, byteCount = 0;

			foreach(BackupItemInfo file in this.Items.Where(item => (item.Type == BackupItemInfo.ItemType.File)))
			{
				fileCount++;
				byteCount += (new FileInfo(Path.Combine(BaseDirectoryPath, file.PathFromBase))).Length;
			}
			BackupSizeInfo totalSize =
				new BackupSizeInfo { fileCount_All = fileCount, fileCount_Unique = fileCount, byteCount_All = byteCount, byteCount_Unique = byteCount };

			return totalSize;
		} // end calculateSize()

	} // end class SourcePathInfo
}