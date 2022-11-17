using System.Security.Cryptography;
using SLASCONEDemo.Csharp.Device.Floating;
using SlasconeClient;

Guid _productId = new Guid("b18657cc-1f7c-43fa-e3a4-08da6fa41ad3");
string _licenseKey = "27180460-29df-4a5a-a0a1-78c85ab6cee0";

SlasconeService slasconeService = new SlasconeService();

CheckLicense();

// License check procedure
// 1. Try to create a license heartbeat
// 2a. License heartbeat successfully created: Store license info and signature
// 2b. If not successfully created with status code 409 and error 2006 (Unknown client): Activate the license
// 2c. Offline or not licensed mode: work with old license infos from a previous license check
void CheckLicense()
{
	// 1. Try to create a license heartbeat
	var status = slasconeService.TryAddLicenseHeartbeat(_productId, out var licenseInfo, out var signature, out var errorId);

	switch (status)
	{
		case 200:
			// License heartbeat successfully created
			Console.WriteLine("Successful created license heartbeat.");
			Success(licenseInfo, signature);

			break;

		case 409:
			// Http status: conflict
			Console.Write("Error occurred while creating a license heartbeat: ");
			switch (errorId)
			{
				case 2006:
					// Slascone error 2006: "Unknown client"
					Console.WriteLine(@"Slascone error ""Unknown client""");
					HandleUnknownClientError();
					break;

				default:
					// Other Slascone error codes do not exist
					Console.WriteLine($"Slascone error {errorId}");
					break;
			}

			break;

		default:
			// Some not specified error occurred: Work in offline or non licensed mode
			Console.Write("Unspecified error occurred while creating a license heartbeat.");
			Offline();

			break;
	}
}

//-------------------------------------------------------------------------------
// 2a. License heartbeat successfully created: Store license info and signature
void Success(LicenseInfoDto licenseInfo, string signature)
{
	Console.WriteLine($"License for {licenseInfo.Customer.Company_name}");

	// TODO: Store license info and signature locally in files to use it in an offline situation

	// Handle license info
	//  o Active and expired state (i.e. valid state)
	//  o Active features and limitations
	Console.WriteLine($"License is {(licenseInfo.Is_license_valid ? "valid" : "not valid")} (IsActive: {licenseInfo.Is_license_active}; IsExpired: {licenseInfo.Is_license_expired})");
	Console.WriteLine($"Active features: {string.Join(", ", licenseInfo.Features.Where(f => f.Is_active).Select(f => f.Name))}");
	Console.WriteLine($"Limitations: {string.Join(", ", licenseInfo.Limitations.Select(l => $"{l.Name} = {l.Value}"))}");
}

//-------------------------------------------------------------------------------
// 2b. If not successfully created with status code 409 and error 2006 (Unknown client): Activate the license
void HandleUnknownClientError()
{
	var status = slasconeService.TryActivateLicense(_productId, _licenseKey, out var licenseInfo, out var signature, out var errorId);

	switch (status)
	{
		case 200:
			// License successfully activated
			Console.WriteLine("Successful activated license.");
			Success(licenseInfo, signature);
			break;

		default:
			// Some not specified error occurred: Work in offline or non licensed mode
			Console.Write($"Unspecified error occurred while creating a license heartbeat. Status: {status}, ErrorId: {errorId}");
			Offline();
			break;
	}
}

//-------------------------------------------------------------------------------
// 2c. Offline or not licensed mode: work with old license infos from a previous license check
void Offline()
{
	// TODO: Retrieve license info an signature from local files
	// TODO: Validate license info with signature
	// TODO: Handle license info as shown in method Success()
}
