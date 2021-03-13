using System;
using LiteDB;
using System.Collections.Generic;

namespace hlback.Database
{
	// GroupRecord:
	/// <summary>Class corresponding to a database record for a group of files all of which point to a single physical copy of the data.</summary>
	class GroupRecord
	{
		[BsonId]
		/// <summary>ID value used in the LiteDB database for this record.</summary>
		public ObjectId ID { get; set; }


		/// <summary>Hash value of the file data contents (all the files in the group being identical).</summary>
		public string Hash { get; init; }

		/// <summary>Stored size of the file (that is, of each [identical] file in the group) at the time the group was created.</summary>
		public long FileSize { get; init; }

		/// <summary>List of objects containing info about each of the files in this group.</summary>
		public List<FileBackupRecord> Files { get; init; }

		/// <summary>Created date of the file (that is of each [identical] file in the group) at the time the group was created.</summary>
		public long CreatedDate_UTC_Ticks { get; init; }
	} // end class GroupRecord
}