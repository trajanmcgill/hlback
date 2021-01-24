using System;

namespace hlback.FileManagement
{
	class BackupSizeInfo
	{
		public long fileCount_All;
		public long fileCount_Unique;
		public long byteCount_All;
		public long byteCount_Unique;


		public static BackupSizeInfo operator +(BackupSizeInfo sizeInfo1, BackupSizeInfo sizeInfo2)
		{
			BackupSizeInfo combinedSizeInfo =
				new BackupSizeInfo()
				{
					fileCount_All = sizeInfo1.fileCount_All + sizeInfo2.fileCount_All,
					fileCount_Unique = sizeInfo1.fileCount_Unique + sizeInfo2.fileCount_Unique,
					byteCount_All = sizeInfo1.byteCount_All + sizeInfo2.byteCount_All,
					byteCount_Unique = sizeInfo1.byteCount_Unique + sizeInfo2.byteCount_Unique
				};
			return combinedSizeInfo;
		} // end operator +()

	} // end class BackupSizeInfo	
}