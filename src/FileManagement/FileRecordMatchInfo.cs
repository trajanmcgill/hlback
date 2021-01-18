using System;
using System.IO;

namespace hlback.FileManagement
{
	class FileRecordMatchInfo
	{
		public string hash;
		public string databaseRecordBasePath;
		public readonly string databaseRecordGroupPath;
		public readonly FileInfo hardLinkTarget;

		public FileRecordMatchInfo(string hash, string databaseRecordBasePath, string databaseRecordGroupPath, FileInfo hardLinkTarget)
		{
			this.hash = hash;
			this.databaseRecordBasePath = databaseRecordBasePath;
			this.databaseRecordGroupPath = databaseRecordGroupPath;
			this.hardLinkTarget = hardLinkTarget;
		} // end FileRecordMatchInfo constructor
		
	} // end class FileRecordMatchInfo
}