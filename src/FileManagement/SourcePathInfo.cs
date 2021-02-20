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
				foreach (BackupItemInfo item in getAllItems())
					yield return item;
			}
		} // end Items property


		private BackupSizeInfo _Size = null;
		private readonly List<(bool, Regex)> Rules;


		public SourcePathInfo(string itemPath, List<string> ruleDefinitions)
		{
			this.BaseItemFullPath = itemPath;
			
			Rules = new List<(bool, Regex)>();
			if (ruleDefinitions != null)
			{
				foreach (string individualRuleDefinition in ruleDefinitions)
				{
					if (individualRuleDefinition.Length < 1)
						continue;
					
					if (individualRuleDefinition[0] == '+')				
						Rules.Add((true, new Regex(individualRuleDefinition.Substring(1))));
					else if (individualRuleDefinition[0] == '-')
						Rules.Add((false, new Regex(individualRuleDefinition.Substring(1))));
					else
						throw new OptionsException($"Invalid rule definition specified: {individualRuleDefinition}");
				}
			}
		} // end SourcePathInfo() constructor


		private IEnumerable<BackupItemInfo> getAllItems()
		{
			if (Directory.Exists(BaseItemFullPath))
			{
				// Item is a directory.
				DirectoryInfo baseItem = new DirectoryInfo(BaseItemFullPath);
				string baseContainerPath = baseItem.Parent.FullName;

				// Return the base item itself as an item to be backed up.
				yield return new BackupItemInfo(BackupItemInfo.ItemType.Directory, baseContainerPath, baseItem.Name);

				// Return every item inside the base directory.
				foreach (BackupItemInfo item in getItems(baseContainerPath, baseItem.Name, true))
					yield return item;
			}
			else if (File.Exists(BaseItemFullPath))
			{
				// Item is a file.
				FileInfo baseItem = new FileInfo(BaseItemFullPath);
				string baseContainerPath = baseItem.Directory.FullName;

				// Return the file item to be backed up.
				yield return new BackupItemInfo(BackupItemInfo.ItemType.File, baseContainerPath, baseItem.Name);
			}
			else // Item doesn't exist at all or isn't accessible
				throw new PathException($"Item not found or not accessible: {BaseItemFullPath}");
		} // end getAllItems()


		private IEnumerable<BackupItemInfo> getItems(string baseContainerPath, string startingPointRelativePath, bool defaultUsability)
		{
			DirectoryInfo currentDirectory = new DirectoryInfo(Path.Combine(baseContainerPath, startingPointRelativePath));
			
			foreach (DirectoryInfo subDirectory in currentDirectory.EnumerateDirectories())
			{
				string subDirectoryPathFromBase = Path.Combine(startingPointRelativePath, subDirectory.Name);
				bool thisSubDirectoryIncluded = itemAllowedByRules(subDirectoryPathFromBase, defaultUsability);

				if (thisSubDirectoryIncluded)
					yield return new BackupItemInfo(BackupItemInfo.ItemType.Directory, baseContainerPath, subDirectoryPathFromBase);

				foreach (BackupItemInfo item in getItems(baseContainerPath, subDirectoryPathFromBase, thisSubDirectoryIncluded))
					yield return item;
			}

			foreach (FileInfo file in currentDirectory.EnumerateFiles())
			{
				string filePathFromBase = Path.Combine(startingPointRelativePath, file.Name);
				if (itemAllowedByRules(filePathFromBase, defaultUsability))
					yield return new BackupItemInfo(BackupItemInfo.ItemType.File, baseContainerPath, filePathFromBase);
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
				byteCount += (new FileInfo(file.FullPath)).Length;
			}
			BackupSizeInfo totalSize =
				new BackupSizeInfo { fileCount_All = fileCount, fileCount_Unique = fileCount, byteCount_All = byteCount, byteCount_Unique = byteCount };

			return totalSize;
		} // end calculateSize()

	} // end class SourcePathInfo
}