using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MijnHost;

public class DnsApiClient : IDisposable
{
	private readonly HttpClient httpClient;

	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	public DnsApiClient(string apiKey, string? userAgent = null)
	{
		httpClient = new HttpClient
		{
			BaseAddress = new Uri("https://mijn.host/api/v2/")
		};

		httpClient.DefaultRequestHeaders.Accept.Add(
			new MediaTypeWithQualityHeaderValue("application/json"));

		httpClient.DefaultRequestHeaders.Add("API-Key", apiKey);
		httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
			userAgent ?? "my-application/1.0.0");
	}

	public async Task<DnsResponse<DnsDomains>> GetDomainsAsync(CancellationToken ct = default)
	{
		var response = await httpClient
			.GetAsync("domains/", ct)
			.ConfigureAwait(false);

		var json = await response.Content.ReadAsStringAsync(ct);

		if (!response.IsSuccessStatusCode)
		{
			var error = JsonSerializer.Deserialize<DnsApiErrorResponse>(json, JsonOptions)
						?? new DnsApiErrorResponse(-1, "Unknown error");

			throw new DnsApiException(response.StatusCode, error.Status, error.StatusDescription, json);
		}

		return JsonSerializer.Deserialize<DnsResponse<DnsDomains>>(json, JsonOptions)!;
	}

	public async Task<DnsResponse<DnsRecords>> GetDomainRecordsAsync(
		string domain,
		CancellationToken ct = default)
	{
		var response = await httpClient
			.GetAsync($"domains/{Uri.EscapeDataString(domain)}/dns", ct)
			.ConfigureAwait(false);

		var json = await response.Content.ReadAsStringAsync(ct);

		if (!response.IsSuccessStatusCode)
		{
			var error = JsonSerializer.Deserialize<DnsApiErrorResponse>(json, JsonOptions)
						?? new DnsApiErrorResponse(-1, "Unknown error");

			throw new DnsApiException(response.StatusCode, error.Status, error.StatusDescription, json);
		}

		return JsonSerializer.Deserialize<DnsResponse<DnsRecords>>(json, JsonOptions)!;
	}

	public async Task<DnsApiResponse> UpdateDomainRecordsAsync(
		string domain,
		IEnumerable<DnsRecord> records,
		CancellationToken ct = default)
	{
		var payload = new { records };
		var json = JsonSerializer.Serialize(payload, JsonOptions);
		var content = new StringContent(json, Encoding.UTF8, "application/json");

		var response = await httpClient
			.PutAsync($"domains/{Uri.EscapeDataString(domain)}/dns", content, ct)
			.ConfigureAwait(false);

		var responseJson = await response.Content.ReadAsStringAsync(ct);

		if (!response.IsSuccessStatusCode)
		{
			var error = JsonSerializer.Deserialize<DnsApiErrorResponse>(responseJson, JsonOptions)
						?? new DnsApiErrorResponse(-1, "Unknown error");

			throw new DnsApiException(response.StatusCode, error.Status, error.StatusDescription, responseJson);
		}

		return JsonSerializer.Deserialize<DnsApiResponse>(responseJson, JsonOptions)!;
	}

	public async Task<DnsApiResponse> PatchDnsRecordAsync(
	string domain,
	DnsRecord record,
	CancellationToken ct = default)
	{
		var payload = new { record };
		var json = JsonSerializer.Serialize(payload, JsonOptions);
		var content = new StringContent(json, Encoding.UTF8, "application/json");

		using var request = new HttpRequestMessage(HttpMethod.Patch, $"domains/{Uri.EscapeDataString(domain)}/dns")
		{
			Content = content
		};

		var response = await httpClient
			.SendAsync(request, ct)
			.ConfigureAwait(false);

		var responseJson = await response.Content.ReadAsStringAsync(ct);

		if (!response.IsSuccessStatusCode)
		{
			var error = JsonSerializer.Deserialize<DnsApiErrorResponse>(responseJson, JsonOptions)
						?? new DnsApiErrorResponse(-1, "Unknown error");

			throw new DnsApiException(response.StatusCode, error.Status, error.StatusDescription, responseJson);
		}

		return JsonSerializer.Deserialize<DnsApiResponse>(responseJson, JsonOptions)!;
	}

	public void Dispose() => httpClient.Dispose();
}

public record DnsRecord(
	string Name,
	string Type,
	string Value,
	int Ttl = 900);

public record DnsResponse<T>(
	int Status,
	string StatusDescription,
	T Data);


public record DnsDomains(
	List<DnsDomain> Domains);

public record DnsDomain(
	int Id,
	string Domain,
	string Renewal_Date,
	string Status,
	int Status_Id,
	List<string> Tags);

public record DnsRecords(
	string Domain,
	List<DnsRecord> Records);

public record DnsApiResponse(
	int Status,
	string StatusDescription);

public record DnsApiErrorResponse(
	int Status,
	string StatusDescription);

public class DnsApiException : Exception
{
	public HttpStatusCode HttpStatusCode { get; }
	public int ApiStatus { get; }
	public string ApiStatusDescription { get; }
	public string RawResponse { get; }

	public DnsApiException(HttpStatusCode httpStatusCode, int apiStatus, string apiStatusDescription, string rawResponse)
		: base($"DNS API error {apiStatus}: {apiStatusDescription}")
	{
		HttpStatusCode = httpStatusCode;
		ApiStatus = apiStatus;
		ApiStatusDescription = apiStatusDescription;
		RawResponse = rawResponse;
	}
}