using Microsoft.Extensions.Configuration;
using MijnHost;


if(args.Length < 1)
{
	Console.WriteLine("Usage:");
	Console.WriteLine("\t--list");
	Console.WriteLine("\t--records example.com");
	Console.WriteLine("\t--challenge example.com 12345");
	Console.WriteLine("\t--cname example.com example.com.acme.certservice.nl.");
	Console.WriteLine("\t--checkall");
	Environment.Exit(1);
}

try
{
	var config = new ConfigurationBuilder()
	.SetBasePath(AppContext.BaseDirectory)
	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
	.Build();

	string apiKey = config["MijnHost:ApiKey"]
		?? throw new InvalidOperationException("MijnHost:ApiKey ontbreekt in appsettings.json");

	string? userAgent = config["MijnHost:UserAgent"];

	using var client = new DnsApiClient(apiKey, userAgent);

	DnsApiResponse? result = null;
	string domain, value;

	switch (args[0])
	{
		case "--list":
			DnsResponse<DnsDomains> dnsResponse = await client.GetDomainsAsync();
			foreach(DnsDomain dom in dnsResponse.Data.Domains)
			{
				Console.WriteLine($"{dom.Domain}");
			}
			break;
		case "--records":
			DnsResponse<DnsRecords> dnsRecords = await client.GetDomainRecordsAsync(args[1]);
			Console.WriteLine($"Records for domain: {dnsRecords.Data.Domain}");
			foreach(DnsRecord record in dnsRecords.Data.Records)
			{
				Console.WriteLine($"{record.Name}\t\t{record.Ttl}\t{record.Type}\t\t{record.Value}");
			}
			break;
		case "--challenge":
			domain = args[1];
			value = args[2];
			result = await client.PatchDnsRecordAsync(domain, new DnsRecord($"_acme-challenge", "TXT", value, 60));
			Console.WriteLine($"Success: {result.Status} - {result.StatusDescription}");
			break;
		case "--cname":
			domain = args[1];
			value = args[2];
			result = await client.PatchDnsRecordAsync(domain, new DnsRecord($"_acme-challenge", "CNAME", value, 60));
			Console.WriteLine($"Success: {result.Status} - {result.StatusDescription}");
			break;
		case "--checkall":
			await CheckAllHelper.CheckAlAsync(client);
			break;
		default:
			Console.WriteLine("Onbekende opdracht.");
			break;
	}
}
catch (DnsApiException ex)
{
	Console.WriteLine($"API fout ({ex.HttpStatusCode}): {ex.ApiStatus} - {ex.ApiStatusDescription}");
}
catch (Exception ex)
{
	Console.WriteLine($"Onverwachte fout: {ex.Message}");
}