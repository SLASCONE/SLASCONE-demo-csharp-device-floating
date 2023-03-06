using SlasconeClient;
using System.Management;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System;
using System.Text;

namespace SLASCONEDemo.Csharp.Device.Floating
{
	internal class Helper
	{
		#region Const

		private const string licenseFile = @"..\..\..\Assets\license.json";
		private const string signatureFile = @"..\..\..\Assets\signature.txt";
		private const string signaturePubKeyPem =
@"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwpigzm+cZIyw6x253YRD
mroGQyo0rO9qpOdbNAkE/FMSX+At5CQT/Cyr0eZTo2h+MO5gn5a6dwg2SYB/K1Yt
yuiKqnaEUfoPnG51KLrj8hi9LoZyIenfsQnxPz+r8XGCUPeS9MhBEVvT4ba0x9Ew
R+krU87VqfI3KNpFQVdLPaZxN4STTEZaet7nReeNtnnZFYaUt5XeNPB0b0rGfrps
y7drmZz81dlWoRcLrBRpkf6XrOTX4yFxe/3HJ8mpukuvdweUBFoQ0xOHmG9pNQ31
AHGtgLYGjbKcW4xYmpDGl0txfcipAr1zMj7X3oCO9lHcFRnXdzx+TTeJYxQX2XVb
hQIDAQAB
-----END PUBLIC KEY-----";

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
		public static bool Validate(string content, string signature, string pubKeyPem)
		{
			using (RSA rsa = RSA.Create())
			{
				rsa.ImportFromPem(pubKeyPem);

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
			if (!Validate(rawLicenseInfo, signature, signaturePubKeyPem))
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

			var valid = Validate(rawLicenseInfo, signature, signaturePubKeyPem);

			licenseInfo = JsonConvert.DeserializeObject<LicenseInfoDto>(rawLicenseInfo);

			return valid;
		}
	}
}