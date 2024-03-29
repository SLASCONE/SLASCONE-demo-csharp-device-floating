﻿using SlasconeClient;
using System.Net;
using System.Net.Http.Headers;

namespace SLASCONEDemo.Csharp.Device.Floating
{
	internal class SlasconeService
	{
		#region Const

		private const string BaseUrl = "https://api.slascone.com";

		// TODO: Exchange the value of the variables to your specific tenant.
		private readonly Guid _isvId = new Guid("2af5fe02-6207-4214-946e-b00ac5309f53");
		private const string _provisioningKey = "NfEpJ2DFfgczdYqOjvmlgP2O/4VlqmRHXNE9xDXbqZcOwXTbH3TFeBAKKbEzga7D7ashHxFtZOR142LYgKWdNocibDgN75/P58YNvUZafLdaie7eGwI/2gX/XuDPtqDW";

		#endregion

		#region Members

		private HttpClient _httpClient;

		private LicensesClient? _licensesClient;

		private ProvisioningClient? _provisioningClient;

		private SessionClient? _sessionClient;

		private readonly string _deviceId;

		private readonly string _userName;

		#endregion

		#region Construction

		public SlasconeService()
		{
			IsvId = _isvId;
			ProvisioningKey = _provisioningKey;

			_deviceId = Helper.GetWindowsUniqueDeviceId();
			_userName = "demo@slascone.com";
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
					_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
					_httpClient.DefaultRequestHeaders.Add("ProvisioningKey", ProvisioningKey);
				}

				return _httpClient;
			}
		}

		private LicensesClient LicensesClient
			=> _licensesClient ??= new LicensesClient(BaseUrl, HttpClient);

		private ProvisioningClient ProvisioningClient
			=> _provisioningClient ??= new ProvisioningClient(BaseUrl, HttpClient);

		private SessionClient SessionClient
			=> _sessionClient ??= new SessionClient(BaseUrl, HttpClient);

		#endregion

		#region Interface

		/// <summary>
		/// Raw License info as received in the Http response. Needed for signature validation
		/// </summary>
		public string RawLicenseInfoDto { get; private set; }

		/// <summary>
		/// Signature for the raw License info
		/// </summary>
		public string LicenseInfoSignature { get; private set; }

		/// <summary>
		/// Try to add a license heartbeat
		/// </summary>
		/// <param name="productId">Product Id of the licensed product</param>
		/// <param name="heartbeatType"></param>
		/// <param name="licenseInfo">License info</param>
		/// <param name="errorId">Slascone error id in case of an error</param>
		/// <returns>Http status code: 200=OK, 409=Conflict</returns>
		public int TryAddLicenseHeartbeat(Guid productId, Guid heartbeatType, out LicenseInfoDto licenseInfo,
			out int errorId)
		{
			if (!TryExecute<SwaggerResponse<LicenseInfoDto>, HeartbeatResponseErrors>(() => ProvisioningClient.HeartbeatsAsync(IsvId, new AddHeartbeatDto()
			    {
				    Product_id = productId,
				    Client_id = _deviceId,
				    Operating_system = Helper.GetWindowsOperatingSystem(),
				    Software_version = "22",
					Heartbeat_type_id = heartbeatType
			    }).Result, out var swaggerResponse, out var status, out errorId))
			{
				licenseInfo = null;
				RawLicenseInfoDto = null;
				LicenseInfoSignature = null;
				return status;
			}

			RawLicenseInfoDto = ProvisioningClient.LastReceivedResponseBody;

			if (swaggerResponse.Headers.TryGetValue("x-slascone-signature", out var headers)
			    && null != headers)
			{
				LicenseInfoSignature = headers.First();
			}
			else
			{
				LicenseInfoSignature = null;
			}

			licenseInfo = swaggerResponse.Result;

			return (int)swaggerResponse.StatusCode;
		}

		/// <summary>
		/// Try to activate a license
		/// </summary>
		/// <param name="productId">Product Id of the licensed product</param>
		/// <param name="licenseKey">License to activate</param>
		/// <param name="licenseInfo">License info</param>
		/// <param name="errorId">Slascone error id in case of an error</param>
		/// <returns>Http status code: 200=OK</returns>
		public int TryActivateLicense(Guid productId, string licenseKey, out LicenseInfoDto licenseInfo,
			out int errorId)
		{
			if (!TryExecute<SwaggerResponse<LicenseInfoDto>, ActivateLicenseResponseErrors>(() => ProvisioningClient.ActivationsAsync(IsvId, new ActivateClientDto
			    {
				    Product_id = productId,
				    License_key = licenseKey,
				    Client_id = _deviceId,
				    Client_description = "SLASCONE-demo-csharp-swagger",
				    Client_name = "SLASCONE demo app",
				    Software_version = "22"
			    }).Result, out var swaggerResponse, out var status, out errorId))
			{
				licenseInfo = null;
				RawLicenseInfoDto = null;
				LicenseInfoSignature = null;
				return status;
			}

			RawLicenseInfoDto = ProvisioningClient.LastReceivedResponseBody;

			if (swaggerResponse.Headers.TryGetValue("x-slascone-signature", out var headers)
			    && null != headers)
			{
				LicenseInfoSignature = headers.First();
			}
			else
			{
				LicenseInfoSignature = null;
			}

			licenseInfo = swaggerResponse.Result;

			return (int)swaggerResponse.StatusCode;
		}

		/// <summary>
		/// Try to open a new session
		/// </summary>
		/// <param name="licenseId">License Id for which a new session should be opened</param>
		/// <param name="sessionId">Id of the new session if successfully opened</param>
		/// <param name="errorId">Slascone error id in case of an error</param>
		/// <returns>Sssion successfully opened</returns>
		public bool TryOpenSession(Guid licenseId, out Guid sessionId, out int errorId)
		{
			var newSessionId = sessionId = Guid.NewGuid();

			if (!(TryExecute<SessionStatusDto, OpenSessionErrors>(() => SessionClient.OpenAsync(IsvId, new SessionRequestDto
			      {
				      Client_id = _deviceId,
				      License_id = licenseId,
				      Session_id = newSessionId,
				      User_id = _userName,
				      Checkout_period = 5.0 // minutes
			      }).Result, out var sessionStatus, out _, out errorId) 
			      && (sessionStatus?.Is_session_valid ?? false)))
			{
				sessionId = Guid.Empty;
				return false;
			}

			return true;
		}

		/// <summary>
		/// Try to renew an open session
		/// </summary>
		/// <param name="licenseId">License Id th which the session belongs</param>
		/// <param name="sessionId">Id of the session that should be renewed</param>
		/// <param name="errorId">Slascone error id in case of an error</param>
		/// <returns>Session sucessfully renewed</returns>
		public bool TryRenewSession(Guid licenseId, Guid sessionId, out int errorId)
		{
			return TryExecute<SessionStatusDto, OpenSessionErrors>(() => SessionClient.OpenAsync(IsvId, new SessionRequestDto
			       {
				       Client_id = _deviceId,
				       License_id = licenseId,
				       Session_id = sessionId,
				       User_id = _userName,
				       Checkout_period = 5.0 // minutes
			       }).Result, out var sessionStatus, out _, out errorId)
			       && (sessionStatus?.Is_session_valid ?? false);
		}

		/// <summary>
		/// Try to close a session
		/// </summary>
		/// <param name="licenseId">License Id th which the session belongs</param>
		/// <param name="sessionId">Id of the session that should be closed</param>
		/// <param name="result">Result of the operation</param>
		/// <param name="errorId">Slascone error id in case of an error</param>
		/// <returns>Session successfully closed</returns>
		public bool TryCloseSession(Guid licenseId, Guid sessionId, out string? result, out int errorId)
		{
			return TryExecute<string, CloseSessionErrors>(() => SessionClient.CloseAsync(IsvId, new SessionRequestDto
			{
				Client_id = _deviceId,
				License_id = licenseId,
				Session_id = sessionId,
				User_id = _userName
			}).Result, out result, out _, out errorId);
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Execute an operation with standard exception handling
		/// </summary>
		/// <typeparam name="T">Type of the result of the operation</typeparam>
		/// <typeparam name="TResult">Type of the exception that can possibly been thrown</typeparam>
		/// <param name="f">Operation to execute</param>
		/// <param name="result">Result of the operation if successfully executed</param>
		/// <param name="status">Status code of the execution (Http status)</param>
		/// <param name="errorId">Slascone error id in case of an error</param>
		/// <returns></returns>
		private bool TryExecute<T, TResult>(Func<T> f, out T? result, out int status, out int errorId)
			where T : class?
			where TResult : BaseErrorResponse
		{
			try
			{
				result = f();

				status = (int)HttpStatusCode.OK;
				errorId = 0;
				return true;
			}
			catch (AggregateException aggregateException)
			{
				switch (aggregateException.InnerException)
				{
					case ApiException<TResult> apiException:
						Console.WriteLine(@$"Slascone error {apiException.Result.Id}: ""{apiException.Result.Message}""");
						status = apiException.StatusCode;
						errorId = apiException.Result.Id;
						break;
					case ApiException commonApiException:
						Console.WriteLine($"Slascone error {commonApiException.Response}");
						status = commonApiException.StatusCode;
						errorId = 0;
						break;
					case HttpRequestException httpException:
						Console.WriteLine(httpException);
						status = (int)httpException.StatusCode;
						errorId = 0;
						break;
					default:
						Console.WriteLine(aggregateException.InnerException);
						status = (int)HttpStatusCode.BadRequest;
						errorId = 0;
						break;
				}

				result = default;
				return false;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);

				result = default;
				status = (int)HttpStatusCode.BadRequest;
				errorId = 0;
				return false;
			}
		}

		#endregion
	}
}
