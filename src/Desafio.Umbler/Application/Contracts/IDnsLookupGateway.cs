using System.Threading;
using System.Threading.Tasks;
using Desafio.Umbler.Application.DTOs;

namespace Desafio.Umbler.Application.Contracts
{
    public interface IDnsLookupGateway
    {
        Task<DnsLookupResult> QueryAsync(string domainName, CancellationToken cancellationToken = default);
    }
}
