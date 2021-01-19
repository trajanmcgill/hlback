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


		private readonly Verbosity verbosity;


		public ConsoleOutput(Verbosity verbosity)
		{
			this.verbosity = verbosity;
		} // end ConsoleOutput constructor


		public void report(string text, Verbosity importance)
		{
			if (importance >= verbosity)
				Console.WriteLine(text);
		} // end report() [overload 1]

		public void report(int indentLevel, string text, Verbosity importance)
		{
			if (importance >= verbosity)
			{
				Console.WriteLine(new String('\t', indentLevel) + text);
			}
		} // end report() [overload 2]

	} // end class ConsoleOutput
}