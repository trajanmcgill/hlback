using System;

namespace hlback
{
	class ConsoleOutput
	{
		public enum Verbosity
		{
			DebugEvents = 0,
			LowImportanceEvents = 1,
			NormalEvents = 2,
			ErrorsAndWarnings = 3
		}

		private const int ReportableProgressDifference = 1;

		private readonly Verbosity verbosity;

		private bool reportingProgress;


		public ConsoleOutput(Verbosity verbosity)
		{
			this.verbosity = verbosity;
			this.reportingProgress = false;
		} // end ConsoleOutput constructor


		public void report(string text, Verbosity importance)
		{
			if (importance >= verbosity)
			{
				if (reportingProgress)
				{
					Console.Write(Environment.NewLine);
					reportingProgress = false;
				}
				Console.WriteLine(text);
			}
		} // end report() [overload 1]

		public void report(int indentLevel, string text, Verbosity importance)
		{
			if (importance >= verbosity)
			{
				if (reportingProgress)
				{
					Console.Write(Environment.NewLine);
					reportingProgress = false;
				}
				Console.WriteLine(new String('\t', indentLevel) + text);
			}
		} // end report() [overload 2]


		public void reportProgress(int newCompletionPercentage, int lastCompletionPercentage, Verbosity importance)
		{
			if (importance >= verbosity)
			{
				reportingProgress = true;
				if (newCompletionPercentage - lastCompletionPercentage > ReportableProgressDifference)
				{
					if (reportingProgress)
						Console.Write(new string ('\b', 14)); // backspaces to the beginning of the line
					Console.Write(newCompletionPercentage.ToString("000") + "% complete.");
				}
			}
		} // end reportProgress()

	} // end class ConsoleOutput
}