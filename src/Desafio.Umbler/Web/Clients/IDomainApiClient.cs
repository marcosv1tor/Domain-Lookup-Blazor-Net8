using System.Threading;
using System.Threading.Tasks;
using Desafio.Umbler.ClientModels;

namespace Desafio.Umbler.Web.Clients
{
    public interface IDomainApiClient
    {
        Task<DomainLookupVm> GetAsync(string domain, CancellationToken cancellationToken = default);
    }
}
