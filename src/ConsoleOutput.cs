using System;

namespace hlback
{
	// ConsoleOutput:
	/// <summary>Class which handles output to the user.</summary>
	class ConsoleOutput
	{
		// Verbosity:
		/// <summary>Defines levels of output importance.</summary>
		public enum Verbosity
		{
			DebugEvents = 0,
			LowImportanceEvents = 1,
			NormalEvents = 2,
			ErrorsAndWarnings = 3
		}

		/// <summary>Constant defining the minimum user-reportable difference in progress.</summary>
		private const int ReportableProgressDifference = 1;

		/// <summary>The output verbosity level this <c>ConsoleOutput</c> object is using.
		private readonly Verbosity verbosity;

		/// <summary><c>Boolean</c> value tracking whether progress reports are in-progress or not.</summary>
		private bool reportingProgress;


		// ConsoleOutput constructor:
		/// <summary>Sets up the new <c>ConsoleOutput</c> object.</summary>
		/// <param name="verbosity">A <c>ConsoleOutput.Verbosity</c> enum value indicating the verbosity level to use.</param>
		public ConsoleOutput(Verbosity verbosity)
		{
			this.verbosity = verbosity;
			this.reportingProgress = false;
		} // end ConsoleOutput constructor


		// report() [overload 1]:
		/// <summary>[overload 1] Takes a report and, if the importance rises to a level at or above the current verbosity level, delivers it to the user.</summary>
		/// <param name="text">A <c>string</c> containing the information to be displayed.</param>
		/// <param name="importance">A <c>ConsoleOutput.Verbosity</c> enum value indicating the importance of the report.</param>
		/// <exception cref="System.IO.IOException">
		/// 	Can occur when Console.Write() or Console.WriteLine() fail - likely due to something like being redirected to a file and the disk being full.
		/// </exception>
		public void report(string text, Verbosity importance)
		{
			// Only if the importance value exceeds this ConsoleOutput object's verbosity level will we write it out to the user.
			if (importance >= verbosity)
			{
				// If progress reports have been going on, however, which all happen on a single line,
				// we need to start a new line and end the progress reporting.
				if (reportingProgress)
				{
					Console.Write(Environment.NewLine);
					reportingProgress = false;
				}

				// Write out the message to the user.
				Console.WriteLine(text);
			}
		} // end report() [overload 1]

		// report() [overload 2]:
		/// <summary>[overload 2] Takes a report and, if the importance rises to a level at or above the current verbosity level, delivers it to the user, indented by the specified amount.</summary>
		/// <param name="indentLevel">An <c>int</c> indicating how many tab stops to indent the output to the user.</param>
		/// <param name="text">A <c>string</c> containing the information to be displayed.</param>
		/// <param name="importance">A <c>ConsoleOutput.Verbosity</c> enum value indicating the importance of the report.</param>
		/// <exception cref="System.IO.IOException">
		/// 	Can occur when Console.Write() or Console.WriteLine() fail - likely due to something like being redirected to a file and the disk being full.
		/// </exception>
		public void report(int indentLevel, string text, Verbosity importance)
		{
			// Only if the importance value exceeds this ConsoleOutput object's verbosity level will we write it out to the user.
			if (importance >= verbosity)
			{
				// If progress reports have been going on, however, which all happen on a single line,
				// we need to start a new line and end the progress reporting.
				if (reportingProgress)
				{
					Console.Write(Environment.NewLine);
					reportingProgress = false;
				}

				// Write out the message to the user, with the appropriate number of tabs prefixed to the output.
				Console.WriteLine(new String('\t', indentLevel) + text);
			}
		} // end report() [overload 2]


		// reportProgress():
		/// <summary>
		/// 	Indicates a completion percentage to the user.
		/// 	Only actually writes anything to the output device if the specified new completion percentage exceeds the specified last reported value
		/// 	by at least the value of the <c>ReportableProgressDifference</c> constant (and, like regular reporting, if
		/// 	the importance level exceeds the verbosity value).
		/// </summary>
		/// <param name="newCompletionPercentage">An <c>int</c> indicating the degree of completion to show.</param>
		/// <param name="newCompletionPercentage">An <c>int</c> indicating the degree of completion shown last time it was reported to the user.</param>
		/// <param name="importance">A <c>ConsoleOutput.Verbosity</c> enum value indicating the importance of the report.</param>
		/// <exception cref="System.IO.IOException">
		/// 	Can occur when Console.Write() or Console.WriteLine() fail - likely due to something like being redirected to a file and the disk being full.
		/// </exception>
		public void reportProgress(int newCompletionPercentage, int lastCompletionPercentage, Verbosity importance)
		{
			// Only if the importance value exceeds this ConsoleOutput object's verbosity level will we write it out to the user.
			if (importance >= verbosity)
			{
				// Set the reportingProgress value to true to indicate we are doing same-line writing without newlines.
				reportingProgress = true;

				// If the difference between new and old values is great enough, go ahead and write out the new value.
				// Erase the line and rewrite it, so that it is a single, increasing text indicator rather than a scrolling series of values.
				if (newCompletionPercentage - lastCompletionPercentage >= ReportableProgressDifference)
				{
					if (reportingProgress)
						Console.Write(new string ('\b', 14)); // backspaces to the beginning of the line
					Console.Write(newCompletionPercentage.ToString("000") + "% complete.");
				}
			} // end if (importance >= verbosity)
		} // end reportProgress()

	} // end class ConsoleOutput
}