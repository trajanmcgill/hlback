using System;

namespace hlback.FileManagement
{
	class LinuxLinker : ILinker
	{
		public void createHardLink(string newLinkFileName, string targetFileName)
		{
			throw new NotImplementedException("LinuxLinker");
		}
	}
}
