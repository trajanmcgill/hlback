using System;

namespace hlback.FileManagement
{
	class WindowsLinker : ILinker
	{
		public void createHardLink(string newLinkFileName, string targetFileName)
		{
			throw new NotImplementedException("WindowsLinker");
		}
	}
}
