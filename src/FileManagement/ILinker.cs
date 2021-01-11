namespace hlback.FileManagement
{
	interface ILinker
	{
		public void createHardLink(string newLinkFileName, string existingTargetFileName);
	}
}