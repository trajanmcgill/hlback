using System;

namespace hlback.Database
{
	class HardLinkMatch
	{
		public LiteDB.ObjectId ID { get; init; }
		public string MatchingFilePath { get; init; }
	}
}