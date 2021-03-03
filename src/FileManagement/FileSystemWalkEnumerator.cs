using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using hlback.ErrorManagement;

namespace hlback.FileManagement
{
	class FileSystemWalkEnumerator : IEnumerator<BackupItemInfo>
	{
		private readonly string StartingPath;
		private readonly RuleSet Rules;
		private FileSystemWalkLevel CurrentLevelSearchData;
		private readonly Stack<FileSystemWalkLevel> SearchStack;
		
		private bool IsReset;
		private bool IsStillIterating;
		private BackupItemInfo CurrentIteratorItem;
		private string BaseContainerPath;


		public BackupItemInfo Current { get { return CurrentIteratorItem; } }
		object IEnumerator.Current { get { return Current; } }


		public FileSystemWalkEnumerator(string startingPath, RuleSet rules)
		{
			this.StartingPath = startingPath;
			this.SearchStack = new Stack<FileSystemWalkLevel>();
			this.Rules = rules ?? new RuleSet();
			Reset();
		} // end FileSystemWalkEnumerator constructor


		public void Dispose() {}


		public void Reset()
		{
			IsReset = true;
			CurrentIteratorItem = null;
			CurrentLevelSearchData = null;
			SearchStack.Clear();
		}


		public bool MoveNext()
		{
			bool returnValue = true;

			if (IsReset)
			{
				IsReset = false;

				IEnumerator<FileInfo> files;
				IEnumerator<DirectoryInfo> subDirectories;
				(CurrentIteratorItem, BaseContainerPath) = getBaseItem(out files, out subDirectories);

				if (CurrentIteratorItem.Type == BackupItemInfo.ItemType.Directory)
				{
					CurrentLevelSearchData = new FileSystemWalkLevel(CurrentIteratorItem.RelativePath, files, subDirectories, true);
					IsStillIterating = true;
				}
				else
					IsStillIterating = false;
			}
			else if (IsStillIterating)
			{
				while (IsStillIterating)
				{
					if (CurrentLevelSearchData.Files.MoveNext())
					{
						FileInfo file = CurrentLevelSearchData.Files.Current;
						string relativePath = Path.Combine(CurrentLevelSearchData.RelativePath, file.Name);
						RuleSet.AllowanceType itemAllowance = Rules.checkPath(relativePath, CurrentLevelSearchData.ThisItemAllowedByRules);
						if (itemAllowance == RuleSet.AllowanceType.Allowed)
						{
							CurrentIteratorItem = new BackupItemInfo(BackupItemInfo.ItemType.File, BaseContainerPath, relativePath);
							break;
						}
						else
							continue;
					}
					else if (CurrentLevelSearchData.SubDirectories.MoveNext())
					{
						DirectoryInfo nextLevelDirectory = CurrentLevelSearchData.SubDirectories.Current;
						string nextLevelRelativePath = Path.Combine(CurrentLevelSearchData.RelativePath, nextLevelDirectory.Name);

						IEnumerator<FileInfo> nextLevelFiles;
						IEnumerator<DirectoryInfo> nextLevelSubDirectories;
						bool ableToReadSubDirectory = tryOpeningDirectory(nextLevelDirectory, out nextLevelFiles, out nextLevelSubDirectories);

						RuleSet.AllowanceType subDirectoryAllowance = Rules.checkPath(nextLevelRelativePath, CurrentLevelSearchData.ThisItemAllowedByRules);
						bool subDirectoryAllowedByRules = (subDirectoryAllowance == RuleSet.AllowanceType.Allowed);

						if (ableToReadSubDirectory && subDirectoryAllowance != RuleSet.AllowanceType.TreeDisallowed)
						{
							SearchStack.Push(CurrentLevelSearchData);
							CurrentLevelSearchData =
								new FileSystemWalkLevel(nextLevelRelativePath, nextLevelFiles, nextLevelSubDirectories, subDirectoryAllowedByRules);
						}

						if (subDirectoryAllowedByRules)
						{
							CurrentIteratorItem =
								new BackupItemInfo(
									ableToReadSubDirectory ? BackupItemInfo.ItemType.Directory : BackupItemInfo.ItemType.UnreadableDirectory,
									BaseContainerPath,
									nextLevelRelativePath);
							break;
						}
						else
							continue;
					}
					else
					{
						// No more files or subdirectories at this level. Pop up to the next level.
						if (!SearchStack.TryPop(out CurrentLevelSearchData))
						{
							// We're all the way to the top level. We've reached the end.
							CurrentIteratorItem = null;
							returnValue = false;
							IsStillIterating = false;
						}
					}
				} // end while (isStillIterating)
			}
			else
				returnValue = false;

			return returnValue;
		} // end MoveNext()


		private (BackupItemInfo item, string containerPath) getBaseItem(out IEnumerator<FileInfo> files, out IEnumerator<DirectoryInfo> subDirectories)
		{
			BackupItemInfo baseItem;
			string containerPath;

			if (File.Exists(StartingPath))
			{
				// Base item is a file.
				FileInfo item = new FileInfo(StartingPath);
				containerPath = item.Directory.FullName;
				baseItem = new BackupItemInfo(BackupItemInfo.ItemType.File, containerPath, item.Name);
				files = null;
				subDirectories = null;
			}
			else if (Directory.Exists(StartingPath))
			{
				// Base item is a directory.
				DirectoryInfo baseDirectory = new DirectoryInfo(StartingPath);
				string relativePath;

				if (baseDirectory.Parent == null)
				{
					containerPath = baseDirectory.FullName;
					relativePath = "";
				}
				else
				{
					containerPath = baseDirectory.Parent.FullName;
					relativePath = baseDirectory.Name;
				}

				if (tryOpeningDirectory(baseDirectory, out files, out subDirectories))
					baseItem = new BackupItemInfo(BackupItemInfo.ItemType.Directory, containerPath, relativePath);
				else
					baseItem = new BackupItemInfo(BackupItemInfo.ItemType.UnreadableDirectory, containerPath, relativePath);
			}
			else // Item doesn't exist at all or isn't accessible
				throw new PathException($"Item not found or not accessible: {StartingPath}");
			
			return (baseItem, containerPath);
		} // end getBaseItem()


		private bool tryOpeningDirectory(DirectoryInfo directory, out IEnumerator<FileInfo> files, out IEnumerator<DirectoryInfo> subDirectories)
		{
			bool outcome;
			try
			{
				files = directory.EnumerateFiles().GetEnumerator();
				subDirectories = directory.EnumerateDirectories().GetEnumerator();
				outcome = true;
			}
			catch (UnauthorizedAccessException)
			{
				files = (new List<FileInfo>()).GetEnumerator();
				subDirectories = (new List<DirectoryInfo>()).GetEnumerator();
				outcome = false;
			}
			return outcome;
		} // end tryOpeningDirectory()

	} // end class FileSystemWalkEnumerator
}