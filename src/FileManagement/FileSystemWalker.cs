using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace hlback.FileManagement
{
	// FileSystemWalker:
	/// <summary>
	/// 	A class allowing walking through a directory tree by enumeration,
	/// 	returning <c>BackupItemInfo</c> objects about each item encountered that matches a particular set of inclusion / exclusion rules.
	/// 	[Implements System.Collections.Generic.IEnumerable]
	/// </summary>
	class FileSystemWalker : IEnumerable<BackupItemInfo>
	{
		/// <summary>Full path of the starting point for the enumerative directory tree search.</summary>
		private readonly string StartingPath;

		/// <summary>Set of inclusion / exclusion rules which should be applied when walking through the tree and looking for items to be included.</summary>
		private readonly RuleSet Rules;


		// GetEnumerator():
		/// <summary>Gets an enumerator that allows iterating through all included items in the directory tree</summary>
		/// <returns>
		/// 	An <c>IEnumerator</c> object of type <c>BackupItemInfo</c>, which can be used to iterate through the items
		/// 	in the directory tree that are to be included according to the rule set.
		/// </returns>
		public IEnumerator<BackupItemInfo> GetEnumerator()
		{	return new FileSystemWalkEnumerator(StartingPath, Rules);	}

		// GetEnumerator() [base class]:
		/// <summary>Hidden, non-typed version of the function needed because <c>IEnumerable</c> is implemented.</summary>
		/// <returns>
		/// 	An <c>IEnumerator</c> object of type <c>BackupItemInfo</c>, which can be used to iterate through the items
		/// 	in the directory tree that are to be included according to the rule set.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator()
		{	return this.GetEnumerator();	}


		// FileSystemWalker constructor:
		/// <summary>Constructs a new <c>FileSystemWalker</c> object.</summary>
		/// <param name="startingPath">A <c>string</c> which contains the full path of this directory.</param>
		/// <param name="rules">A <c>RuleSet</c> object containing all the inclusion / exclusion rules to be applied when enumerating included objects within this directory.</param>
		public FileSystemWalker(string startingPath, RuleSet rules)
		{
			this.StartingPath = startingPath;
			this.Rules = rules;
		} // end FileSystemWalker()

	} // end class FileSystemWalker
}