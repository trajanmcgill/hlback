using System;
using LiteDB;
using System.Collections.Generic;

namespace hlback.Database
{
	class GroupRecord
	{
		private readonly long _CreatedDate;
		private readonly int _Age;

		[BsonId]
		public ObjectId ID { get; set; }

		public string Hash { get; init; }
		public long FileSize { get; init; }
		public List<FileBackupRecord> Files { get; init; }
		public long CreatedDate_UTC_Ticks
		{
			get	=> _CreatedDate;

			init
			{
				_CreatedDate = value;
				_Age = (int)DateTime.Now.Subtract(new DateTime((long)_CreatedDate, DateTimeKind.Utc)).TotalDays;
			}
		}

		[BsonIgnore]
		public int Age { get => _Age; }

	} // end class GroupRecord
}