using System.Threading;
using System.Threading.Tasks;
using Desafio.Umbler.Domain.Entities;

namespace Desafio.Umbler.Application.Contracts
{
    public interface IDomainRepository
    {
        Task<DomainRecord?> GetByNameAsync(string normalizedDomainName, CancellationToken cancellationToken = default);

        Task AddAsync(DomainRecord domainRecord, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
