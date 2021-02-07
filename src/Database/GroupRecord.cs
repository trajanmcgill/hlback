using System;
using LiteDB;
using System.Collections.Generic;

namespace hlback.Database
{
	class GroupRecord
	{
		[BsonId]
		public ObjectId ID { get; set; }

		public string Hash { get; init; }
		public long FileSize { get; init; }
		public List<FileBackupRecord> Files { get; init; }
		public long CreatedDate_UTC_Ticks { get; init; }
	} // end class GroupRecord
}