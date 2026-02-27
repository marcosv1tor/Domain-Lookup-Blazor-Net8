using System.Threading;
using System.Threading.Tasks;
using Desafio.Umbler.Application.DTOs;

namespace Desafio.Umbler.Application.Contracts
{
    public interface IWhoisGateway
    {
        Task<WhoisLookupResult> QueryAsync(string query, CancellationToken cancellationToken = default);
    }
}
