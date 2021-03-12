using System;
using System.Collections.Generic;
using System.IO;

namespace hlback.FileManagement
{
	// FileSystemWalkLevel:
	/// <summary>
	/// 	Corresponds to a single directory level on a walk / search through a directory tree.
	/// 	Encapsulates: The relative path from some base starting point, enumerators for the contained files and subdirectories,
	/// 	and whether or not this particular directory itself is to be included or excluded in the set of results.
	/// </summary>
	class FileSystemWalkLevel
	{
		/// <summary>Used for storing the path, relative to some base starting point.</summary>
		public readonly string RelativePath;

		/// <summary>Used to store an enumerator which returns the files in this directory.</summary>
		public readonly IEnumerator<FileInfo> Files;

		/// <summary>Used to store an enumerator which returns the subdirectories in this directory.</summary>
		public readonly IEnumerator<DirectoryInfo> SubDirectories;

		/// <summary>Used to store whether or not this specific directory is to be included in the set of results.</summary>
		public readonly bool ThisItemAllowedByRules;

		
		// FileSystemWalkLevel constructor:
		/// <summary>Constructor, sets up a <c>FileSystemWalkLevel</c> object.</summary>
		/// <param name="relativePath">A <c>string</c> which contains the full path of this directory, relative to some base path.</param>
		/// <param name="files">An <c>IEnumerator</c> of type <c>FileInfo</c> which should allow iterating through all the files in this directory.</param>
		/// <param name="subDirectories">An <c>IEnumerator</c> of type <c>DirectoryInfo</c> which should allow iterating through all the subdirectories in this directory.</param>
		/// <param name="itemAllowedByRules">A <c>bool</c> indicating whether this specific directory is meant to be included in the set of results from walking the tree.</param>
		public FileSystemWalkLevel(string relativePath, IEnumerator<FileInfo> files, IEnumerator<DirectoryInfo> subDirectories, bool itemAllowedByRules)
		{
			this.RelativePath = relativePath;
			this.Files = files;
			this.SubDirectories = subDirectories;
			this.ThisItemAllowedByRules = itemAllowedByRules;
		}
	} // end class FileSystemWalkLevel
}