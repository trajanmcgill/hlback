namespace hlback.FileManagement
{
	interface ILinker
	{
		void createHardLink(string newLinkFileName, string targetFileName);
	}
}