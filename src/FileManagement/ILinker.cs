namespace hlback.FileManagement
{
	// ILinker:
	/// <summary>Exposes a <c>createHardLink()</c> method, for creating a hard link to a file.</summary>
	interface ILinker
	{
		public void createHardLink(string newLinkFileName, string existingTargetFileName);
	}
}