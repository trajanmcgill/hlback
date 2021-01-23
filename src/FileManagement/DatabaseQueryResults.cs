using System;
using System.IO;

namespace hlback.FileManagement
{
	class DatabaseQueryResults
	{
		public readonly string newRecordFileName;
		public readonly string newRecordFilePath;
		public readonly FileInfo bestHardLinkTarget;

		public DatabaseQueryResults(string newRecordFilePath, string newRecordFileName, FileInfo bestHardLinkTarget)
		{
			this.newRecordFilePath = newRecordFilePath;
			this.newRecordFileName = newRecordFileName;
			this.bestHardLinkTarget = bestHardLinkTarget;
		} // end DatabaseQueryResults constructor
		
	} // end class DatabaseQueryResults
}