using Microsoft.Extensions.Configuration;
using MijnHost;


if(args.Length < 2)
{
	Console.WriteLine("Usage: example.com 12345");
	Environment.Exit(1);
}

try
{
	var domain = args[0];
	var value = args[1];
	var config = new ConfigurationBuilder()
	.SetBasePath(AppContext.BaseDirectory)
	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
	.Build();

	string apiKey = config["MijnHost:ApiKey"]
		?? throw new InvalidOperationException("MijnHost:ApiKey ontbreekt in appsettings.json");

	string? userAgent = config["MijnHost:UserAgent"];

	using var client = new DnsApiClient(apiKey, userAgent);

	DnsApiResponse result = await client.PatchDnsRecordAsync(domain, new DnsRecord($"_acme-challenge", "TXT", value, 60));

	Console.WriteLine($"Success: {result.Status} - {result.StatusDescription}");
}
catch (DnsApiException ex)
{
	Console.WriteLine($"API fout ({ex.HttpStatusCode}): {ex.ApiStatus} - {ex.ApiStatusDescription}");
}
catch (Exception ex)
{
	Console.WriteLine($"Onverwachte fout: {ex.Message}");
}