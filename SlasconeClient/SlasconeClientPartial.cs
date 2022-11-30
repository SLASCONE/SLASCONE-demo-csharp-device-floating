using System.Text;

namespace SlasconeClient
{
	public class LastReceivedResponseBodyClient
	{
		public string LastReceivedResponseBody { get; private set; }

		internal async Task PrepareRequestAsync(HttpClient? client, HttpRequestMessage? request, StringBuilder? urlBuilder,
			CancellationToken cancellationToken)
		{
			await Task.FromResult(0);
		}

		internal async Task PrepareRequestAsync(HttpClient? client, HttpRequestMessage? request, string? url,
			CancellationToken cancellationToken)
		{
			await Task.FromResult(0);
		}

		internal async Task ProcessResponseAsync(HttpClient? client, HttpResponseMessage? response,
			CancellationToken cancellationToken)
		{
			LastReceivedResponseBody = await response.Content.ReadAsStringAsync(cancellationToken);
		}
	}

	public partial class Customer_portalClient : LastReceivedResponseBodyClient
	{
	}

	public partial class CustomerClient : LastReceivedResponseBodyClient
	{
	}

	public partial class LicenseClient : LastReceivedResponseBodyClient
	{
	}

	public partial class CountClient : LastReceivedResponseBodyClient
	{
	}

	public partial class Token_keyClient : LastReceivedResponseBodyClient
	{
	}

	public partial class ProductClient : LastReceivedResponseBodyClient
	{
	}

	public partial class AnalyticalClient : LastReceivedResponseBodyClient
	{
	}

	public partial class Data_exchangeClient : LastReceivedResponseBodyClient
	{
	}

	public partial class CustomersClient : LastReceivedResponseBodyClient
	{
	}

	public partial class LicensesClient : LastReceivedResponseBodyClient
	{
	}

	public partial class Data_gatheringClient : LastReceivedResponseBodyClient
	{
	}

	public partial class Consumption_heartbeatsClient : LastReceivedResponseBodyClient
	{
	}

	public partial class LookupClient : LastReceivedResponseBodyClient
	{
	}

	public partial class ProvisioningClient : LastReceivedResponseBodyClient
	{
	}

	public partial class ActivationsClient : LastReceivedResponseBodyClient
	{
	}

	public partial class ValidateClient : LastReceivedResponseBodyClient
	{
	}

	public partial class StateClient : LastReceivedResponseBodyClient
	{
	}

	public partial class SessionClient : LastReceivedResponseBodyClient
	{
	}
}