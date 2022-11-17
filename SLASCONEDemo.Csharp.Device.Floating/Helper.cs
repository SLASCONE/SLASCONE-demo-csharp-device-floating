using System.Management;

namespace SLASCONEDemo.Csharp.Device.Floating
{
	internal class Helper
	{
		/// <summary>
		/// Get operating system name and version
		/// </summary>
		/// <returns>Windows name and version</returns>
		public static string GetWindowsOperatingSystem()
		{
			using (var searcher = new ManagementObjectSearcher("SELECT Name, Version FROM Win32_OperatingSystem"))
			{
				var shares = searcher.Get();
				var props = shares.Cast<ManagementObject>().First().Properties;
				var name = props["Name"].Value as string;
				var version = props["Version"].Value as string;

				return $"{name.Split('|').First()} {version}";
			}
		}

		/// <summary>
		/// Get a unique device id based on the system
		/// </summary>
		/// <returns>UUID via string</returns>
		public static string GetWindowsUniqueDeviceId()
		{
			using (var searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct"))
			{
				var shares = searcher.Get();
				var props = shares.Cast<ManagementObject>().First().Properties;
				var uuid = props["UUID"].Value as string;

				return uuid;
			}
		}
	}
}
