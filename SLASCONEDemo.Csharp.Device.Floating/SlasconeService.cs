using SlasconeClient;
using System.Net;
using System.Net.Mail;

namespace SLASCONEDemo.Csharp.Device.Floating
{
	internal class SlasconeService
	{
		#region Const

		private const string BaseUrl = "https://api.slascone.com";
		private readonly Guid _isvId = new Guid("2af5fe02-6207-4214-946e-b00ac5309f53");
		private const string provisioningKey = "NfEpJ2DFfgczdYqOjvmlgP2O/4VlqmRHXNE9xDXbqZcOwXTbH3TFeBAKKbEzga7D7ashHxFtZOR142LYgKWdNocibDgN75/P58YNvUZafLdaie7eGwI/2gX/XuDPtqDW";

		#endregion

		#region Members

		private HttpClient _httpClient;

		private LicensesClient _licensesClient;

		private ProvisioningClient _provisioningClient;

		#endregion

		#region Construction

		public SlasconeService()
		{
			IsvId = _isvId;
			ProvisioningKey = provisioningKey;
		}

		#endregion

		#region Properties

		private Guid IsvId { get; }
		
		private string ProvisioningKey { get; }

		private HttpClient HttpClient
		{
			get
			{
				if (null == _httpClient)
				{
					_httpClient = new HttpClient();
					_httpClient.DefaultRequestHeaders.Add("ProvisioningKey", ProvisioningKey);
				}

				return _httpClient;
			}
		}

		private LicensesClient LicensesClient
			=> _licensesClient ?? (_licensesClient = new LicensesClient(HttpClient) { BaseUrl = BaseUrl });

		private ProvisioningClient ProvisioningClient
			=> _provisioningClient ?? (_provisioningClient = new ProvisioningClient(HttpClient) { BaseUrl = BaseUrl });

		#endregion

		#region Interface

		/// <summary>
		/// Try to add a license heartbeat
		/// </summary>
		/// <param name="productId">Product Id of the licensed product</param>
		/// <param name="licenseInfo">License info</param>
		/// <param name="signature">Signature to verify the license info</param>
		/// <param name="errorId">Slascone error id in case of an error</param>
		/// <returns>Http status code: 200=OK, 409=Conflict</returns>
		public int TryAddLicenseHeartbeat(Guid productId, out LicenseInfoDto licenseInfo, out string signature, out int errorId)
		{
			Console.WriteLine("Trying to create a license heartbeat ...");

			int status = 0;
			errorId = 0;

			try
			{
				var response = ProvisioningClient.HeartbeatsAsync(IsvId, new AddHeartbeatDto()
				{
					Product_id = productId,
					Client_id = Helper.GetWindowsUniqueDeviceId(),
					Operating_system = Helper.GetWindowsOperatingSystem(),
					Software_version = "22"
				}).Result;

				if (HttpStatusCode.OK != (HttpStatusCode)response.StatusCode)
				{
					licenseInfo = null;
					signature = null;
					return (int)response.StatusCode;
				}

				if (response.Headers.TryGetValue("x-slascone-signature", out var headers)
					&& null != headers)
				{
					signature = headers.First();
				}
				else
				{
					signature = null;
				}

				if (null != response.Result)
				{
					licenseInfo = response.Result;
					return (int)response.StatusCode;
				}
			}
			catch (AggregateException aggregateException)
			{
				if (aggregateException.InnerException is ApiException<HeartbeatResponseErrors> apiException)
				{
					errorId = apiException.Result.Id;
					status = apiException.StatusCode;
				}
				else if (aggregateException.InnerException is HttpRequestException httpException)
				{
					Console.WriteLine(httpException);
					status = (int)(httpException.StatusCode ?? HttpStatusCode.BadRequest);
				}
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception);
				status = (int)HttpStatusCode.BadRequest;
			}

			licenseInfo = null;
			signature = null;
			return status;
		}

		/// <summary>
		/// Try to activate a license
		/// </summary>
		/// <param name="productId">Product Id of the licensed product</param>
		/// <param name="licenseKey">License to activate</param>
		/// <param name="licenseInfo">License info</param>
		/// <param name="signature">Signature to verify the license info</param>
		/// <param name="errorId">Slascone error id in case of an error</param>
		/// <returns></returns>
		public int TryActivateLicense(Guid productId, string licenseKey, out LicenseInfoDto licenseInfo, out string signature, out int errorId)
		{
			Console.WriteLine("Trying to activate the license ...");

			int status = 0;
			errorId = 0;

			try
			{
				var response = ProvisioningClient.ActivationsAsync(IsvId, new ActivateClientDto
				{
					Product_id = productId,
					License_key = licenseKey,
					Client_id = Helper.GetWindowsUniqueDeviceId(),
					Client_description = "SLASCONE-demo-csharp-swagger",
					Client_name = "SLASCONE demo app",
					Software_version = "22"
				}).Result;

				if (HttpStatusCode.OK != (HttpStatusCode)response.StatusCode)
				{
					licenseInfo = null;
					signature = null;
					errorId = 0;
					return (int)response.StatusCode;
				}

				if (response.Headers.TryGetValue("x-slascone-signature", out var headers)
					&& null != headers)
				{
					signature = headers.First();
				}
				else
				{
					signature = null;
				}

				if (null != response.Result)
				{
					licenseInfo = response.Result;
					errorId = 0;
					return (int)response.StatusCode;
				}
			}
			catch (AggregateException aggregateException)
			{
				if (aggregateException.InnerException is ApiException<HeartbeatResponseErrors> apiException)
				{
					errorId = apiException.Result.Id;
					status = apiException.StatusCode;
				}
				else if (aggregateException.InnerException is HttpRequestException httpException)
				{
					Console.WriteLine(httpException);
					status = (int)(httpException.StatusCode ?? HttpStatusCode.BadRequest);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				status = (int)HttpStatusCode.BadRequest;
			}

			licenseInfo = null;
			signature = null;
			return status;
		}

		#endregion
	}
}
