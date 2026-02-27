using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Desafio.Umbler.Application.Contracts;
using Desafio.Umbler.Application.DTOs;
using DnsClient;

namespace Desafio.Umbler.Infrastructure.External
{
    public sealed class DnsLookupGateway : IDnsLookupGateway
    {
        private readonly ILookupClient _lookupClient;

        public DnsLookupGateway(ILookupClient lookupClient)
        {
            _lookupClient = lookupClient;
        }

        public async Task<DnsLookupResult> QueryAsync(string domainName, CancellationToken cancellationToken = default)
        {
            var response = await _lookupClient.QueryAsync(
                domainName,
                QueryType.ANY,
                QueryClass.IN,
                cancellationToken);

            var aRecord = response.Answers.ARecords().FirstOrDefault();
            var ip = aRecord?.Address?.ToString() ?? string.Empty;
            var ttl = aRecord?.TimeToLive ?? 0;

            var nameServers = response.Answers
                .NsRecords()
                .Select(ns => ns.NSDName.Value)
                .Where(ns => !string.IsNullOrWhiteSpace(ns))
                .Select(ns => ns.Trim().TrimEnd('.').ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return new DnsLookupResult
            {
                Ip = ip,
                Ttl = ttl,
                NameServers = nameServers
            };
        }
    }
}
