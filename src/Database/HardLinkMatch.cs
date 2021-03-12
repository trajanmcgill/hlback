using System;

namespace hlback.Database
{
	// HardLinkMatch:
	/// <summary>Class encapsulating a file match found in the database.</summary>
	class HardLinkMatch
	{
		/// <summary>ID of the corresponding entry in the LiteDB database.</summary>
		public LiteDB.ObjectId ID { get; init; }

		/// <summary>Full path of the existing file which is a match.</summary>
		public string MatchingFilePath { get; init; }
	}
}