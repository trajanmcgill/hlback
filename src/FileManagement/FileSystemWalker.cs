using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace hlback.FileManagement
{
	class FileSystemWalker : IEnumerable<BackupItemInfo>
	{
		private readonly string StartingPath;
		private readonly RuleSet Rules;

		public IEnumerator<BackupItemInfo> GetEnumerator()
		{	return new FileSystemWalkEnumerator(StartingPath, Rules);	}

		IEnumerator IEnumerable.GetEnumerator()
		{	return this.GetEnumerator();	}


		public FileSystemWalker(string startingPath, RuleSet rules)
		{
			this.StartingPath = startingPath;
			this.Rules = rules;
		}

	} // end class FileSystemWalker
}