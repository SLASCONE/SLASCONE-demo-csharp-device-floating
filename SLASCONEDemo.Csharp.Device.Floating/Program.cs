using System.Security.Cryptography;
using SLASCONEDemo.Csharp.Device.Floating;
using SlasconeClient;

Guid _productId = new Guid("b18657cc-1f7c-43fa-e3a4-08da6fa41ad3");
string _licenseKey = "27180460-29df-4a5a-a0a1-78c85ab6cee0";

//
// These heartbeat types are only examples. You can define heartbeat types according to your requirements.
//
Guid _programStartHeartbeat = new Guid("6f163dd8-325a-400b-95f8-ad4697cef1fb");
Guid _programExitHeartbeat = new Guid("0c2251b3-1670-4428-8fcb-24347937eee8");

SlasconeService slasconeService = new SlasconeService();

if (CheckLicense(out var licenseInfo))
{
	if (OpenSession(licenseInfo.License_key, out var sessionId, out var errorId))
	{
		//------------------------------//
		// Do the applications work now //
		//------------------------------//
		Console.WriteLine("Doing some work ...");
		Thread.Sleep(TimeSpan.FromSeconds(3));

		RenewSession(licenseInfo.License_key, sessionId);

		Console.WriteLine("Doing some more work ...");
		Thread.Sleep(TimeSpan.FromSeconds(3));

		CloseSession(licenseInfo.License_key, sessionId);

		// Optional: Send a program exit heartbeat
		slasconeService.TryAddLicenseHeartbeat(_productId, _programExitHeartbeat, out _, out _, out _);
	}
	else if (1007 == errorId)
	{
		// The number of allowed connections has been reached.
		Console.WriteLine("The number of allowed connections has been reached. Please try again later");
	}
}
else
{
	// 2c. Offline or not licensed mode if heartbeat creation and/or license activation not successful:
	//     Use old license infos from a previous license check
	// TODO: Retrieve license info and signature from local files
	// TODO: Validate license info with signature
	// TODO: Handle license info as shown in method Success()
	return;
}

//
// Trying to exceed the session limit
// (you would not do this in a real life scenario)
//

var openSessionIds = new List<Guid>();

while (OpenSession(licenseInfo.License_key, out var sessionId, out _))
{
	openSessionIds.Add(sessionId);
}

Console.WriteLine("Press any key to continue ...");
Console.ReadKey();

foreach (var openSessionId in openSessionIds)
{
	CloseSession(licenseInfo.License_key, openSessionId);
}

#region Methods / functions

// License check procedure
// 1. Try to create a license heartbeat
// 2a. License heartbeat successfully created: Store license info and signature
// 2b. If not successfully created with status code 409 and error 2006 (Unknown client): Try to activate the license
bool CheckLicense(out LicenseInfoDto licenseInfo)
{
	// 1. Try to create a license heartbeat
	Console.WriteLine("Trying to create a license heartbeat ...");

	var status = slasconeService.TryAddLicenseHeartbeat(_productId, _programStartHeartbeat, out licenseInfo, out var signature, out var errorId);

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
				case 2002:
					Console.WriteLine(@"Slascone error ""This token is not assigned.""");
					break;
				case 2003:
					Console.WriteLine(@"Slascone error ""Unknown token license key.""");
					break;
				case 2006:
					Console.WriteLine(@"Slascone error ""Unknown client.""");
					if (HandleUnknownClientError(out licenseInfo, out signature))
					{
						Success(licenseInfo, signature);

						// Optional: Send a license heartbeat immediately after activation
						status = slasconeService.TryAddLicenseHeartbeat(_productId, _programStartHeartbeat, out licenseInfo,
							out signature, out errorId);
					}
					break;
				case 3000:
					Console.WriteLine(@"Slascone error ""Unknown product.""");
					break;
				case 5000:
					Console.WriteLine(@"Slascone error ""This client is assigned to a different token license key. Use the right or no token license key.""");
					break;
				case 5001:
					Console.WriteLine(@"Slascone error ""Unknown heartbeat type.""");
					break;
				default:
					Console.WriteLine($"Slascone error {errorId}");
					break;
			}

			break;

		default:
			// Some not specified error occurred: Work in offline or non licensed mode
			Console.Write("Unspecified error occurred while creating a license heartbeat.");

			break;
	}

	return licenseInfo?.Is_license_valid ?? false;
}

//-------------------------------------------------------------------------------
// 2a. License heartbeat successfully created: Store license info and signature
void Success(LicenseInfoDto licenseInfo, string signature)
{
	Console.WriteLine("License infos:");
	Console.WriteLine($"   Company name: {licenseInfo.Customer.Company_name}");

	// TODO: Store license info and signature locally in files to use it in an offline situation

	// Handle license info
	//  o Active and expired state (i.e. valid state)
	//  o Active features and limitations
	Console.WriteLine($"   License is {(licenseInfo.Is_license_valid ? "valid" : "not valid")} (IsActive: {licenseInfo.Is_license_active}; IsExpired: {licenseInfo.Is_license_expired})");
	Console.WriteLine($"   Active features: {string.Join(", ", licenseInfo.Features.Where(f => f.Is_active).Select(f => f.Name))}");
	Console.WriteLine($"   Limitations: {string.Join(", ", licenseInfo.Limitations.Select(l => $"{l.Name} = {l.Value}"))}");
}

//-------------------------------------------------------------------------------
// 2b. If not successfully created with status code 409 and error 2006 (Unknown client): Activate the license
bool HandleUnknownClientError(out LicenseInfoDto licenseInfo, out string signature)
{
	Console.WriteLine("Trying to activate the license ...");

	var status = slasconeService.TryActivateLicense(_productId, _licenseKey, out licenseInfo, out signature, out var errorId);

	switch (status)
	{
		case 200:
			// License successfully activated
			Console.WriteLine("Successful activated license.");
			break;

		case 409:
			// Http status: conflict
			Console.Write("Error occurred while activating the license: ");
			switch (errorId)
			{
				case 1000:
					Console.WriteLine("The input is not a valid license key.");
					break;
				case 1001:
					Console.WriteLine("The license is expired.");
					break;
				case 1002:
					Console.WriteLine("The license is not activated.");
					break;
				case 1003:
					Console.WriteLine("Non compliant software version.");
					break;
				case 2000:
					Console.WriteLine("This master license has no available tokens. You can either add token(s) to this license or unassign some existing token(s).");
					break;
				case 2001:
					Console.WriteLine("Token already assigned.");
					break;
				case 3000:
					Console.WriteLine("Unknown product.");
					break;
			}

			break;

		default:
			// Some not specified error occurred
			Console.Write($"Unspecified error occurred while creating a license heartbeat. Status: {status}, ErrorId: {errorId}");
			break;
	}

	return 200 == status;
}

bool OpenSession(string licenseKey, out Guid sessionId, out int errorId)
{
	Console.WriteLine("Trying to open a session ...");

	if (!slasconeService.TryOpenSession(Guid.Parse(licenseKey), out sessionId, out errorId))
	{
		Console.Write("Failed to open a session: ");
		switch (errorId)
		{
			case 1000:
				Console.WriteLine("The input is not a valid license key.");
				break;
			case 1001:
				Console.WriteLine("The license is expired.");
				break;
			case 1002:
				Console.WriteLine("The license is not activated.");
				break;
			case 1007:
				Console.WriteLine("The number of allowed connections has been reached.");
				break;
			case 2002:
				Console.WriteLine("This token is not assigned.");
				break;
			case 2006:
				Console.WriteLine("Unknown client.");
				break;
			case 12000:
				Console.WriteLine("The provisioning mode of this license is set to Named. Sessions need the provisioning mode to be Floating.");
				break;
			case 12002:
				Console.WriteLine("The maximum number of floating sessions with same client has been reached. Can not open an additional session.");
				break;
			case 12003:
				Console.WriteLine("The checkout period has to be a positive number (in minutes).");
				break;
			case 12004:
				Console.WriteLine("A user id is mandatory for this license type");
				break;
			case 12005:
				Console.WriteLine("A session id is mandatory for this license type");
				break;
			case 12006:
				Console.WriteLine("A session with the same user is already active");
				break;
		}

		return false;
	}

	Console.WriteLine($"Successfully opened session {sessionId}");
	return true;
}

bool RenewSession(string licenseKey, Guid sessionId)
{
	Console.WriteLine("Trying to renew the session ...");

	if (!slasconeService.TryRenewSession(Guid.Parse(licenseKey), sessionId, out var errorId))
	{
		Console.WriteLine($"Failed to renew session. Error {errorId}");
		return false;
	}

	Console.WriteLine($"Successfully renewed session {sessionId}");
	return true;
}

void CloseSession(string licenseKey, Guid sessionId)
{
	Console.WriteLine("Trying to close the session ...");

	if (!slasconeService.TryCloseSession(Guid.Parse(licenseKey), sessionId, out var result, out var errorId))
	{
		Console.WriteLine(@$"Failed to close session {sessionId}. Error {errorId}: ""{result}""");
	}

	Console.WriteLine(@$"Session {sessionId} successfully closed. ""{result}""");
}

#endregion
