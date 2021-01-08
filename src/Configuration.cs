using System;
using System.Runtime.InteropServices;

namespace hlback
{
	class Configuration
	{
		public enum SystemType { Linux, Windows };

		public Configuration()
		{}

		public SystemType systemType
		{
			get
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					return SystemType.Windows;
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
					return SystemType.Linux;
				else
					throw new NotImplementedException("Unsupported operating system.");
			}
		}
	}
}