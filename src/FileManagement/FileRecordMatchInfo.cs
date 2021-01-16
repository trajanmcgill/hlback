using System;
using System.IO;

namespace hlback.FileManagement
{
	class FileRecordMatchInfo
	{
		public string hash;
		public readonly FileInfo hardLinkTarget;
		public string databaseRecordFullPath;

		public FileRecordMatchInfo(string hash, FileInfo hardLinkTarget, string databaseRecordFullPath)
		{
			this.hash = hash;
			this.hardLinkTarget = hardLinkTarget;
			this.databaseRecordFullPath = databaseRecordFullPath;
		} // end FileRecordMatchInfo constructor
		
	} // end class FileRecordMatchInfo
}