using System;

namespace hlback.FileManagement
{
	// BackupSizeInfo:
	/// <summary>
	/// 	A class which contains information about the size of a set of files and directories for backup, measured in both bytes and number of items.
	/// 	Allows tracking totals, totals of unique files, and totals of skipped files.
	/// 	Overloads the <c>+</c> operator so as to allow easy summing of <c>BackupSizeInfo</c> objects.
	/// </summary>
	class BackupSizeInfo
	{
		/// <summary>Total number of included files.</summary>
		public long fileCount_All = 0;

		/// <summary>Number of included unique files.</summary>
		public long fileCount_Unique = 0;

		/// <summary>Total number of files skipped.</summary>
		public long fileCount_Skip = 0;

		/// <summary>Total number of bytes of all included files combined.</summary>
		public long byteCount_All = 0;

		/// <summary>Bytes of all included unique files combined.</summary>
		public long byteCount_Unique = 0;

		/// <summary>Bytes of all skipped file combined.</summary>
		public long byteCount_Skip = 0;


		// operator +:
		/// <summary>Operator overload for the + operator.</summary>
		/// <returns>A new BackupSizeInfo object containing the summed values from each of two BackupSizeInfo objects</returns>
		public static BackupSizeInfo operator +(BackupSizeInfo sizeInfo1, BackupSizeInfo sizeInfo2)
		{
			BackupSizeInfo combinedSizeInfo =
				new BackupSizeInfo()
				{
					fileCount_All = sizeInfo1.fileCount_All + sizeInfo2.fileCount_All,
					fileCount_Unique = sizeInfo1.fileCount_Unique + sizeInfo2.fileCount_Unique,
					fileCount_Skip = sizeInfo1.fileCount_Skip + sizeInfo2.fileCount_Skip,
					byteCount_All = sizeInfo1.byteCount_All + sizeInfo2.byteCount_All,
					byteCount_Unique = sizeInfo1.byteCount_Unique + sizeInfo2.byteCount_Unique,
					byteCount_Skip = sizeInfo1.byteCount_Skip + sizeInfo2.byteCount_Skip
				};
			return combinedSizeInfo;
		} // end operator +()

	} // end class BackupSizeInfo	
}