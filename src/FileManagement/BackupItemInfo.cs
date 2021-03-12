using System;
using System.IO;

namespace hlback.FileManagement
{
	// BackupItemInfo:
	/// <summary>Encapsulates the data relevant to a single item in a backup job.</summary>
	class BackupItemInfo
	{
		// ItemType:
		/// <summary>Defines the different types of items that can exist.</summary>
		public enum ItemType { Directory, UnreadableDirectory, File }
		
		/// <summary>The type of item this object corresponds to.</summary>
		public readonly ItemType Type;

		/// <summary>The path of the item, relative to some base path.</summary>
		public readonly string RelativePath;

		/// <summary>The full path of the item.</summary>
		public readonly string FullPath;

		
		// BackupItemInfo constructor:
		/// <summary>Initializes the object.</summary>
		/// <param name="type">An <c>ItemType</c> value indicating the type of item this is.</param>
		/// <param name="basePath">The base path from which items' relative paths are being calculated.</param>
		/// <param name="relativePath">The path of this item relative to the specified base path.</param>
		/// <exception cref="ArgumentException">Thrown when one of the path arguments is invalid.</exception>
		/// <exception cref="ArgumentNullException">Thrown when one of the path arguments is <c>null</c>.</exception>
		public BackupItemInfo(ItemType type, string basePath, string relativePath)
		{
			this.Type = type;
			this.RelativePath = relativePath;
			this.FullPath = Path.Combine(basePath, relativePath);
		} // end BackupItemInfo() constructor
		
	} // end class BackupItemInfo
}