using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using hlback.ErrorManagement;

namespace hlback.FileManagement
{
	// SourcePathInfo:
	/// <summary>Class encapsulating data corresponding to a single source path for a backup job.</summary>
	class SourcePathInfo
	{
		/// <summary>Full source path string</summary>
		public readonly string BaseItemFullPath;
		
		/// <summary>Inclusion/Exclusion rules that govern this backup source</summary>
		private readonly RuleSet Rules;


		// SourcePathInfo constructor:
		/// <summary>Constructor for a SourcePathInfo object.</summary>
		/// <param name="itemPath">A <c>string</c> with the full path of the source object (directory or file).</param>
		/// <param name="rules">A <c>RuleSet</c> object containing all the inclusion/exclusion rules that are to be applied to this source.</param>
		public SourcePathInfo(string itemPath, RuleSet rules)
		{
			this.BaseItemFullPath = itemPath;
			this.Rules = rules;
		} // end SourcePathInfo() constructor


		// calculateSize():
		/// <summary>Calculates the entire size of all the items within the backup source.</summary>
		/// <returns>
		/// 	A <c>BackupSizeInfo</c> object containing total number of files and bytes within this source, respecting the inclusion / exclusion rules.
		/// 	This function does not try to de-duplicate-- the returned object has fileCount_All and fileCount_Unique set the same,
		/// 	and byteCount_All and byteCount_Unique are identical as well.
		/// </returns>
		public BackupSizeInfo calculateSize()
		{
			// Set file and byte counter variables to zero, then iterate through all the files,
			// adding up numbers and sizes.
			long fileCount = 0, byteCount = 0;
			foreach(BackupItemInfo file in getAllItems().Where(item => (item.Type == BackupItemInfo.ItemType.File)))
			{
				fileCount++;
				byteCount += (new FileInfo(file.FullPath)).Length;
			}

			// Return the size information.
			BackupSizeInfo totalSize =
				new BackupSizeInfo { fileCount_All = fileCount, fileCount_Unique = fileCount, byteCount_All = byteCount, byteCount_Unique = byteCount };
			return totalSize;
		} // end calculateSize()


		// getAllItems():
		/// <summary>Gets an enumerable collection of all the items within this source, according to its inclusion / rules.</summary>
		/// <returns>An <c>IEnumerable</c> of <c>BackupItemInfo</c> objects corresponding to all the files and directories in this source.</returns>
		public IEnumerable<BackupItemInfo> getAllItems()
		{
			return new FileSystemWalker(BaseItemFullPath, Rules);
		} // end getAllItems()

	} // end class SourcePathInfo
}