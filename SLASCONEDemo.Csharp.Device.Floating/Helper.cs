using SlasconeClient;
using System.Management;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System;
using System.Text;

namespace SLASCONEDemo.Csharp.Device.Floating
{
	internal class Helper
	{
		#region Const

		private const string certificateFile = @"..\..\..\Assets\signature_pub_key.pfx";
		private const string licenseFile = @"..\..\..\Assets\license.json";
		private const string signatureFile = @"..\..\..\Assets\signature.txt";

		#endregion

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

		/// <summary>
		/// Validate the signature of a Slascone response with the public key
		/// </summary>
		/// <param name="content">Content from the Http response</param>
		/// <param name="signature">Signature (from the http response header "x-slascone-signature")</param>
		/// <param name="certFile"></param>
		/// <returns></returns>
		public static bool Validate(string content, string signature, string certFile)
		{
			using (var signatureKeyCert = new X509Certificate2(certFile))
			using (RSA rsa = signatureKeyCert.GetRSAPublicKey())
			{
				Console.WriteLine($"Verifying with signature '{signatureKeyCert.Subject}'");
				Console.WriteLine($"   Serial: {signatureKeyCert.SerialNumber}");
				Console.WriteLine($"   Thumbprint: {signatureKeyCert.Thumbprint}");

				var signatureValue = Convert.FromBase64String(signature);
				var valid = rsa.VerifyData(Encoding.UTF8.GetBytes(content), signatureValue, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

				Console.WriteLine($"   Result: {(valid ? "Valid" : "Not valid")}");
				return valid;
			}
		}

		/// <summary>
		/// Store license info and signature in local files
		/// </summary>
		/// <param name="rawLicenseInfo">License info as received from the Http response</param>
		/// <param name="signature">Signature (from the Http response header "x-slascone-signature")</param>
		public static void StoreLicenseAndSignature(string rawLicenseInfo, string signature)
		{
			if (!Validate(rawLicenseInfo, signature, certificateFile))
				return;

			using (StreamWriter writer = new StreamWriter(licenseFile))
				writer.Write(rawLicenseInfo);

			using (StreamWriter writer = new StreamWriter(signatureFile))
				writer.Write(signature);
		}

		/// <summary>
		/// Get license info from locally stored files and validate signature
		/// </summary>
		/// <param name="licenseInfo">License info from file</param>
		/// <returns>Result of signature validation</returns>
		public static bool GetOfflineLicense(out LicenseInfoDto licenseInfo)
		{
			string rawLicenseInfo;
			using (StreamReader reader = new StreamReader(licenseFile))
				rawLicenseInfo = reader.ReadToEnd();

			string signature;
			using (StreamReader reader = new StreamReader(signatureFile))
				signature = reader.ReadToEnd();

			var valid = Validate(rawLicenseInfo, signature, certificateFile);

			licenseInfo = JsonConvert.DeserializeObject<LicenseInfoDto>(rawLicenseInfo);

			return valid;
		}
	}
}