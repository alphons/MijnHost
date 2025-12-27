using System;
using System.Collections.Generic;
using System.Text;

namespace MijnHost;

public class CheckAllHelper
{
	public static async Task CheckAlAsync(DnsApiClient client)
	{
		DnsResponse<DnsDomains> dnsResponse = await client.GetDomainsAsync();
		foreach (DnsDomain dom in dnsResponse.Data.Domains)
		{
			DnsResponse<DnsRecords> dnsRecords = await client.GetDomainRecordsAsync(dom.Domain);

			DnsRecord? challenge = dnsRecords.Data.Records.FirstOrDefault(x => x.Name.StartsWith("_acme-challenge"));
			if (challenge != null)
			{
				Console.WriteLine($"{challenge.Name} -> {challenge.Value}");
			}
			else
			{
				DnsApiResponse? result = await client.PatchDnsRecordAsync(
					dom.Domain, new DnsRecord($"_acme-challenge", "CNAME", $"{dom.Domain}.acme.certservice.nl.", 60));
				if(result.Status == 200)
				{
					Console.WriteLine($"Added _acme-challenge for {dom.Domain}");
				}
				else
				{
					Console.WriteLine($"Failed to add _acme-challenge for {dom.Domain}: {result.StatusDescription}");
				}
			}
		}
	}
}
