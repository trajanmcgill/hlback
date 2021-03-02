using System;

namespace hlback.FileManagement
{
	class BackupSizeInfo
	{
		public long fileCount_All = 0;
		public long fileCount_Unique = 0;
		public long fileCount_Skip = 0;
		public long byteCount_All = 0;
		public long byteCount_Unique = 0;
		public long byteCount_Skip = 0;


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