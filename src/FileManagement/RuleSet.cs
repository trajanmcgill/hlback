using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using hlback.ErrorManagement;

namespace hlback.FileManagement
{
	// RuleSet:
	/// <summary>Class which encapulates the data for a set of file and directory inclusion and exclusion rules.</summary>
	class RuleSet
	{
		// AllowanceType:
		/// <summary>
		/// 	Defines types of rules.
		/// 	Allowed: Files and directories matching this rule are included.
		/// 	Disallowed: Files and directories matching this rule are excluded, but (for a directory) files and directories within them may be included by other rules.
		/// 	TreeDisallowed: Files and directories matching this rule are excluded completely and directories are not traversed at all or searched for any deeper files or directories to include.
		/// </summary>
		public enum AllowanceType { Allowed, Disallowed, TreeDisallowed }


		/// <summary>
		/// 	A <c>List</c> of tuples defining rules.
		/// 	Each one comprises an <c>AllowanceType</c>, defining the type of rule (Allowed, Disallowed, or TreeDisallowed),
		/// 	and a <c>Regex</c>, which is run against a file or directory's path (relative to the base source path, not to the root) to see if the rule matches and should be applied.
		/// </summary>
		private List<(AllowanceType, Regex)> Rules;
		
		
		// RuleSet constructor:
		/// <summary>Constructor which initializes an empty RuleSet object.</summary>
		public RuleSet()
		{
			Rules = new List<(AllowanceType, Regex)>();
		} // end RuleSet constructor


		// addRule():
		/// <summary>Adds a new inclusion or exclusion rule.</summary>
		/// <param name="ruleType">
		/// 	A <c>RuleSet.AllowanceType</c> value setting the type of rule. Possible values:
		/// 		Allowed: Files and directories matching this rule are included.
		/// 		Disallowed: Files and directories matching this rule are excluded, but (for a directory) files and directories within them may be included by other rules.
		/// 		TreeDisallowed: Files and directories matching this rule are excluded completely and directories are not traversed at all or searched for any deeper files or directories to include.
		/// </param>
		/// <param name="ruleDefinition">
		/// 	A <c>Regex</c> which is compared to item's paths (including filename), relative to the base source path, to see if the rule matches and should be applied.
		/// </param>
		public void addRule(AllowanceType ruleType, Regex ruleDefinition)
		{
			Rules.Add((ruleType, ruleDefinition));
		} // end addRule()


		// checkPath():
		/// <summary>
		/// 	Ascertains whether a given path should be allowed under this ruleset.
		/// 	Rules are applied in the same order as added to the ruleset, so later rules can override earlier ones.
		/// 	A default allowability is specified by the caller for if no rule matches.
		/// </summary>
		/// <returns>
		/// 	A <c>RuleSet.AllowanceType</c> value indicating the allowability of the given path under this ruleset.
		/// 	If no rules match the path, returns <c>Allowed</c> if <c>defaultUsability</c> is <c>true</c> and <c>Disallowed</c> if not.
		/// </returns>
		/// <param name="path">A <c>string</c> containing the path to check against the ruleset.</param>
		/// <param name="defaultUsability">A <c>bool</c> determining if the path should be allowed in the case where no rules match.</param>
		/// <exception cref="ArgumentNullException">Thrown when <c>path</c> is null.</exception>
		/// <exception cref="System.Text.RegularExpressions.RegexMatchTimeoutException">Thrown when running the rule's regular expression against the specified path times out.</exception>
		public AllowanceType checkPath(string path, bool defaultUsability)
		{
			AllowanceType returnValue = defaultUsability ? AllowanceType.Allowed : AllowanceType.Disallowed;

			foreach ((AllowanceType allowance, Regex expression) rule in Rules)
			{
				if (rule.expression.IsMatch(path))
					returnValue = rule.allowance;
			}

			return returnValue;
		} // end checkPath()
		
	} // end class Rule
}