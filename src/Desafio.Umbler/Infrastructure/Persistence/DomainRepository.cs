using System.Threading;
using System.Threading.Tasks;
using Desafio.Umbler.Application.Contracts;
using Desafio.Umbler.Domain.Entities;
using Desafio.Umbler.Models;
using Microsoft.EntityFrameworkCore;

namespace Desafio.Umbler.Infrastructure.Persistence
{
    public sealed class DomainRepository : IDomainRepository
    {
        private readonly DatabaseContext _databaseContext;

        public DomainRepository(DatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }

        public async Task<DomainRecord?> GetByNameAsync(string normalizedDomainName, CancellationToken cancellationToken = default)
        {
            return await _databaseContext.Domains
                .FirstOrDefaultAsync(d => d.Name == normalizedDomainName, cancellationToken);
        }

        public async Task AddAsync(DomainRecord domainRecord, CancellationToken cancellationToken = default)
        {
            await _databaseContext.Domains.AddAsync(domainRecord, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _databaseContext.SaveChangesAsync(cancellationToken);
        }
    }
}
