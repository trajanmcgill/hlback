namespace hlback.FileManagement
{
	// ILinker:
	/// <summary>Exposes a <c>createHardLink()</c> method, for creating a hard link to a file.</summary>
	interface ILinker
	{
		// createHardLink():
		/// <summary>Creates a hard link, with the specified name, to the specified existing file. [Implements ILinker.createHardLink()]</summary>
		/// <param name="newLinkFileName">A <c>string</c> with the full name (path + name) of the new link to be created.</param>
		/// <param name="existingTargetFileName">A <c>string</c> containing the full name (path + name) of an existing file whose physical data will be linked to.</param>
		// CHANGE CODE HERE: Add documentation of exceptions thrown.
		public void createHardLink(string newLinkFileName, string existingTargetFileName);
	}
}