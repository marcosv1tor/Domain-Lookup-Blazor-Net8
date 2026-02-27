using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Desafio.Umbler.Application.Contracts;
using Desafio.Umbler.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Desafio.Umbler.Controllers
{
    [ApiController]
    [Route("api/domain")]
    public class DomainController : ControllerBase
    {
        private readonly IDomainLookupService _domainLookupService;

        public DomainController(IDomainLookupService domainLookupService)
        {
            _domainLookupService = domainLookupService;
        }

        [HttpGet("{domainName}")]
        public async Task<ActionResult<DomainLookupResponseDto>> Get(
            [FromRoute]
            [Required(ErrorMessage = "Domain is required.")]
            [StringLength(253, ErrorMessage = "Domain must have at most 253 characters.")]
            [RegularExpression(@"^[^.\s]+\..+$", ErrorMessage = "Domain must include a valid TLD (example: umbler.com).")]
            string domainName,
            CancellationToken cancellationToken)
        {
            var response = await _domainLookupService.GetAsync(domainName, cancellationToken);
            return Ok(response);
        }
    }
}
