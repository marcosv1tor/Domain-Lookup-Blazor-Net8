using System.Threading;
using System.Threading.Tasks;
using Desafio.Umbler.Application.Contracts;
using Desafio.Umbler.Application.DTOs;
using Whois.NET;

namespace Desafio.Umbler.Infrastructure.External
{
    public sealed class WhoisGateway : IWhoisGateway
    {
        public async Task<WhoisLookupResult> QueryAsync(string query, CancellationToken cancellationToken = default)
        {
            var response = await WhoisClient.QueryAsync(query);

            return new WhoisLookupResult
            {
                Raw = response.Raw ?? string.Empty,
                OrganizationName = response.OrganizationName ?? string.Empty
            };
        }
    }
}
