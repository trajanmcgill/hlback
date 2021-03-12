using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using hlback.ErrorManagement;

namespace hlback.FileManagement
{
	// FileSystemWalkEnumerator:
	/// <summary>
	/// 	A class which enumerates through a directory tree, returning <c>BackupItemInfo</c> objects
	/// 	about each item encountered that matches a particular set of inclusion / exclusion rules.
	/// 	[Implements System.Collections.Generic.IEnumerator]
	/// </summary>
	class FileSystemWalkEnumerator : IEnumerator<BackupItemInfo>
	{
		#region Private Member Variables

		/// <summary>Stores the base path for the directory tree enumeration.</summary>
		private readonly string StartingPath;
		
		/// <summary>Stores the set of file and directory inclusion / exclusion rules.</summary>
		private readonly RuleSet Rules;
		
		/// <summary>Holds the subdirectory currently being examined.</summary>
		private FileSystemWalkLevel CurrentLevelSearchData;
		
		/// <summary>
		/// 	Holds a stack of subdirectories currently being examined, so we can complete one,
		/// 	then pop back up to another, and then push it back on the stack to go down another level.
		/// </summary>
		private readonly Stack<FileSystemWalkLevel> SearchStack;
		
		/// <summary>Tracks whether the enumaration has been reset to its starting state.</summary>
		private bool IsReset;

		/// <summary>Tracks whether the enumeration is still going (false if it has gone past then last item).</summary>
		private bool IsStillIterating;

		/// <summary>Used to hold the path for the directory containing the starting path (or the path itself, if at the root level).</summary>
		private string BaseContainerPath;

		#endregion


		#region Properties

		/// <summary>Returns the current item in the enumeration.</summary>
		public BackupItemInfo Current { get; private set; }

		/// <summary>Returns the current item in the enumeration (non-typed version).</summary>
		object IEnumerator.Current { get { return Current; } }

		#endregion


		#region Public Methods

		// FileSystemWalkEnumerator constructor:
		/// <summary>Constructor which sets up a new <c>FileSystemWalkEnumerator</c> object.</summary>
		/// <param name="startingPath">A <c>string</c> which contains the full path of this directory.</param>
		/// <param name="rules">A <c>RuleSet</c> object containing all the inclusion / exclusion rules to be applied when enumerating included objects within this directory.</param>
		public FileSystemWalkEnumerator(string startingPath, RuleSet rules)
		{
			// Set up internal member variables.
			this.StartingPath = startingPath;
			this.Rules = rules ?? new RuleSet(); // Don't let Rules be null; set it to an empty RuleSet if null was passed in.
			this.SearchStack = new Stack<FileSystemWalkLevel>(); // Create an empty stack to hold the levels of our depth-first walk.

			// Set internal state to be ready for the first call to MoveNext().
			Reset();
		} // end FileSystemWalkEnumerator constructor


		// Dispose():
		/// <summary>
		/// 	Disposer for the class, needed because <c>IEnumerator</c> is <c>IDisposable</c>.
		/// 	Doesn't actually do anything, though, because this class does not hold unmanaged resources.
		/// </summary>
		public void Dispose() {}


		// Reset():
		/// <summary>Sets the enumerator back to an initial state, where MoveNext() will move to the very first item.</summary>
		public void Reset()
		{
			IsReset = true;
			Current = null;
			CurrentLevelSearchData = null;
			SearchStack.Clear();
		} // end Reset()


		// MoveNext():
		/// <summary>Moves the iterator to the next item.</summary>
		/// <returns>
		/// 	A <c>bool</c> indicating whether there was, in fact, a next item.
		/// 	Upon success, <c>Current</c> will contain the next item.
		/// 	If the return value is <c>false</c>, there was no next item and <c>Current</c> will be <c>null</c>.
		/// 	Calling this method repeatedly after passing the end of the iteration will have no effect (it will just keep returning <c>false</c>).
		/// </returns>
		public bool MoveNext()
		{
			bool returnValue = true; // Set up the return value to default to true.

			if (IsReset)
			{
				// IsReset was true, so we are about to move to the very first item.

				IsReset = false; // We aren't going to be reset anymore after this.

				// The very first item is the base item itself.
				// Set the current item to an object corresponding to that, but in the process
				// we also need to look up the path containing that item, and get enumerators for the files and subdirectories inside the base item, if any.
				IEnumerator<FileInfo> files;
				IEnumerator<DirectoryInfo> subDirectories;
				(Current, BaseContainerPath) = getBaseItem(out files, out subDirectories);

				if (Current.Type == BackupItemInfo.ItemType.Directory)
				{
					// The base item is a readable directory, so set the current search level to that directory and set the still-iterating state to true.
					CurrentLevelSearchData = new FileSystemWalkLevel(Current.RelativePath, files, subDirectories, true);
					IsStillIterating = true;
				}
				else
					IsStillIterating = false; // Base item is a file. There is nothing further to enumerate.
			}
			else if (IsStillIterating)
			{
				// IsReset was false, but IsStillIterating was true. So we are in the middle of an enumeration.

				// Iterate over the files and subdirectories, checking each against the inclusion / exclusion rules,
				// until we 1) find an included item to return, at which point we set the current item to that and break from the loop;
				// or 2) run out of things to iterate through and IsStillIterating becomes false.
				while (IsStillIterating)
				{
					// We will enumerate through the files in this directory first, then subdirectories.

					if (CurrentLevelSearchData.Files.MoveNext())
					{
						// There is still at least one unexamined file in this directory.

						// Having moved to the next file, get its path and check it against the rules.
						FileInfo file = CurrentLevelSearchData.Files.Current;
						string relativePath = Path.Combine(CurrentLevelSearchData.RelativePath, file.Name);
						RuleSet.AllowanceType itemAllowance = Rules.checkPath(relativePath, CurrentLevelSearchData.ThisItemAllowedByRules); // CHANGE CODE HERE: handle exceptions
						if (itemAllowance == RuleSet.AllowanceType.Allowed)
						{
							// The file is included according to the rules, so set the current iterator item to point to it, and break from the loop to exit the function.
							Current = new BackupItemInfo(BackupItemInfo.ItemType.File, BaseContainerPath, relativePath); // CHANGE CODE HERE: Handle exceptions thrown by BackupItemInfo constructor
							break;
						}
						else
							continue; // This file is not included according to the rules. Loop back and look at the next one.
					}
					else if (CurrentLevelSearchData.SubDirectories.MoveNext())
					{
						// There is still at least one unexamined subdirectory in this directory.

						// Having moved to the next subdirectory, get a DirectoryInfo object corresponding to it,
						// and determine its path relative to the base path.
						DirectoryInfo nextLevelDirectory = CurrentLevelSearchData.SubDirectories.Current;
						string nextLevelRelativePath = Path.Combine(CurrentLevelSearchData.RelativePath, nextLevelDirectory.Name);

						// Try reading the subdirectory, and get enumerators for iterating the files and subdirectories within it.
						IEnumerator<FileInfo> nextLevelFiles;
						IEnumerator<DirectoryInfo> nextLevelSubDirectories;
						bool ableToReadSubDirectory = tryOpeningDirectory(nextLevelDirectory, out nextLevelFiles, out nextLevelSubDirectories);

						// Check the path of the subdirectory against the inclusion / exclusion rules.
						RuleSet.AllowanceType subDirectoryAllowance = Rules.checkPath(nextLevelRelativePath, CurrentLevelSearchData.ThisItemAllowedByRules); // // CHANGE CODE HERE: handle exceptions
						bool subDirectoryAllowedByRules = (subDirectoryAllowance == RuleSet.AllowanceType.Allowed);

						// If we are able to read the subdirectory, we will traverse it, too, in our enumeration,
						// UNLESS the rules say it is an entire tree that is to be excluded (which is different from disallowing
						// just that subdirectory itself but potentially allowing things within it).
						// Push the current directory onto the stack of those currently being searched, so we come back to it,
						// and make the subdirectory the one to look at the next time through the loop.
						if (ableToReadSubDirectory && subDirectoryAllowance != RuleSet.AllowanceType.TreeDisallowed)
						{
							SearchStack.Push(CurrentLevelSearchData);
							CurrentLevelSearchData =
								new FileSystemWalkLevel(nextLevelRelativePath, nextLevelFiles, nextLevelSubDirectories, subDirectoryAllowedByRules);
						}

						// If the subdirectory is allowed by the rules, set it as the current item of this enumerator, and break from the loop to exit the function.
						if (subDirectoryAllowedByRules)
						{
							// CHANGE CODE HERE: Handle exceptions thrown by BackupItemInfo constructor
							Current =
								new BackupItemInfo(
									ableToReadSubDirectory ? BackupItemInfo.ItemType.Directory : BackupItemInfo.ItemType.UnreadableDirectory,
									BaseContainerPath,
									nextLevelRelativePath);
							break;
						}
						else
							continue; // This subdirectory isn't included according to the rules. Loop back and look at the next one.
					}
					else
					{
						// No more files or subdirectories at this level. Pop up to the next level.
						if (!SearchStack.TryPop(out CurrentLevelSearchData))
						{
							// We're all the way to the top level. We've reached the end.
							Current = null;
							returnValue = false;
							IsStillIterating = false;
						}
					}
				} // end while (isStillIterating)
			}
			else
				returnValue = false; // Not reset or still iterating, so we've already moved past the end. Return false.

			return returnValue;
		} // end MoveNext()

		#endregion


		#region Private Methods

		// getBaseItem()
		/// <summary>Gets information about the base item in the this enumerator.</summary>
		/// <returns>
		/// 	A tuple containing a <c>BackupItemInfo</c> object corresponding to the base item itself
		/// 	and a <c>string</c> containing the full path of the directory containing the base item-- or the base item full path, if the base item has no container (is a root directory).
		/// </returns>
		/// <param name="files">An <c>out</c> parameter which will contain an enumerator of all the files within the base item, or <c>null</c> if the base item is a file.</param>
		/// <param name="subDirectories">An <c>out</c> parameter which will contain an enumerator of all the subdirectories within the base item, or <c>null</c> if the base item is a file.</param>
		private (BackupItemInfo item, string containerPath) getBaseItem(out IEnumerator<FileInfo> files, out IEnumerator<DirectoryInfo> subDirectories)
		{
			BackupItemInfo baseItem;
			string containerPath;

			if (File.Exists(StartingPath))
			{
				// Base item is a file.
				
				// Get the info to return about the base item, with the file's path and a BackupItemInfo object corresponding to it.
				FileInfo item = new FileInfo(StartingPath);
				containerPath = item.Directory.FullName;
				baseItem = new BackupItemInfo(BackupItemInfo.ItemType.File, containerPath, item.Name); // CHANGE CODE HERE: Handle exceptions thrown by BackupItemInfo constructor
				
				// Both of the out parameters will be null, since a file contains no files or subdirectories.
				files = null;
				subDirectories = null;
			}
			else if (Directory.Exists(StartingPath))
			{
				// Base item is a directory.

				// Get the info to return about the base item.
				DirectoryInfo baseDirectory = new DirectoryInfo(StartingPath);
				string relativePath;
				if (baseDirectory.Parent == null)
				{
					// If the item has no parent (is a root directory), then the container is set to the item itself,
					// and the item's relative path is an empty string.
					containerPath = baseDirectory.FullName;
					relativePath = "";
				}
				else
				{
					// In the ordinary case, there is a container directory, so get its full path,
					// and the item's path relative to that is just its name.
					containerPath = baseDirectory.Parent.FullName;
					relativePath = baseDirectory.Name;
				}

				// Try reading the directory that is the base item.
				// If we can, the BackupItemInfo object returned will be marked as being Directory.
				// If we can't, it will be marked as being an unreadable directory so we don't try to traverse it.
				if (tryOpeningDirectory(baseDirectory, out files, out subDirectories))
					baseItem = new BackupItemInfo(BackupItemInfo.ItemType.Directory, containerPath, relativePath); // CHANGE CODE HERE: Handle exceptions thrown by BackupItemInfo constructor
				else
					baseItem = new BackupItemInfo(BackupItemInfo.ItemType.UnreadableDirectory, containerPath, relativePath); // CHANGE CODE HERE: Handle exceptions thrown by BackupItemInfo constructor
			}
			else // Item doesn't exist at all or isn't accessible
				throw new PathException($"Item not found or not accessible: {StartingPath}");
			
			return (baseItem, containerPath);
		} // end getBaseItem()


		// tryOpeningDirectory()
		/// <summary>Attempts to open and read the specified directory.</summary>
		/// <returns>A <c>bool</c> indicating whether the directory could be read.</returns>
		/// <param name="directory">A <c>DirectoryInfo</c> object corresponding to the directory to try to read.</param>
		/// <param name="files">An <c>out</c> parameter which will contain an enumerator of all the files within the specified directory.</param>
		/// <param name="subDirectories">An <c>out</c> parameter which will contain an enumerator of all the subdirectories within the specified directory.</param>
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
				// Getting enumerators for the files and directories in a directory failed with an UnauthorizedAccessException.
				// We don't have access to this directory. Set the out parameters to empty lists, and return false.
				files = (new List<FileInfo>()).GetEnumerator();
				subDirectories = (new List<DirectoryInfo>()).GetEnumerator();
				outcome = false;
			}
			return outcome;
		} // end tryOpeningDirectory()

		#endregion

	} // end class FileSystemWalkEnumerator
}