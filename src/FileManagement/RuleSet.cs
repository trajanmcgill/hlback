using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using hlback.ErrorManagement;

namespace hlback.FileManagement
{
	class RuleSet
	{
		public enum AllowanceType { Allowed, Disallowed, TreeDisallowed }


		private List<(AllowanceType, Regex)> Rules;
		
		
		public RuleSet()
		{
			Rules = new List<(AllowanceType, Regex)>();
		} // end RuleSet constructor


		public void addRule(AllowanceType ruleType, Regex ruleDefinition)
		{
			Rules.Add((ruleType, ruleDefinition));
		} // end addRule()


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