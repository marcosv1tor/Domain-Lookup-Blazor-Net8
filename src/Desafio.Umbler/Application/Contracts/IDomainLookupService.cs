using System.Threading;
using System.Threading.Tasks;
using Desafio.Umbler.Application.DTOs;

namespace Desafio.Umbler.Application.Contracts
{
    public interface IDomainLookupService
    {
        Task<DomainLookupResponseDto> GetAsync(string domainName, CancellationToken cancellationToken = default);
    }
}
