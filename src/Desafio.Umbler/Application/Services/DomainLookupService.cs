using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Desafio.Umbler.Application.Contracts;
using Desafio.Umbler.Application.DTOs;
using Desafio.Umbler.Application.Exceptions;
using Desafio.Umbler.Application.Validation;
using Desafio.Umbler.Domain.Entities;

namespace Desafio.Umbler.Application.Services
{
    public sealed class DomainLookupService : IDomainLookupService
    {
        private static readonly Regex WhoisNameServerRegex = new(
            "(?im)^\\s*(?:name server|nserver)\\s*:\\s*(?<value>\\S+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly IDomainRepository _domainRepository;
        private readonly IDnsLookupGateway _dnsLookupGateway;
        private readonly IWhoisGateway _whoisGateway;
        private readonly IClock _clock;

        public DomainLookupService(
            IDomainRepository domainRepository,
            IDnsLookupGateway dnsLookupGateway,
            IWhoisGateway whoisGateway,
            IClock clock)
        {
            _domainRepository = domainRepository;
            _dnsLookupGateway = dnsLookupGateway;
            _whoisGateway = whoisGateway;
            _clock = clock;
        }

        public async Task<DomainLookupResponseDto> GetAsync(string domainName, CancellationToken cancellationToken = default)
        {
            if (!DomainNameValidator.TryNormalize(domainName, out var normalizedDomain, out var validationError))
            {
                throw new DomainValidationException(validationError);
            }

            var cachedDomain = await _domainRepository.GetByNameAsync(normalizedDomain, cancellationToken);
            if (cachedDomain is not null && !IsExpired(cachedDomain))
            {
                return MapToResponse(cachedDomain, source: "cache", dnsNameServers: Array.Empty<string>());
            }

            var whoisResponse = await _whoisGateway.QueryAsync(normalizedDomain, cancellationToken);
            var dnsResponse = await _dnsLookupGateway.QueryAsync(normalizedDomain, cancellationToken);

            var hostedAt = string.Empty;
            if (!string.IsNullOrWhiteSpace(dnsResponse.Ip))
            {
                var hostedWhoisResponse = await _whoisGateway.QueryAsync(dnsResponse.Ip, cancellationToken);
                hostedAt = hostedWhoisResponse.OrganizationName ?? string.Empty;
            }

            var domainRecord = cachedDomain ?? new DomainRecord();
            if (cachedDomain is null)
            {
                await _domainRepository.AddAsync(domainRecord, cancellationToken);
            }

            domainRecord.Name = normalizedDomain;
            domainRecord.Ip = dnsResponse.Ip ?? string.Empty;
            domainRecord.UpdatedAt = _clock.UtcNow;
            domainRecord.WhoIs = whoisResponse.Raw ?? string.Empty;
            domainRecord.Ttl = dnsResponse.Ttl;
            domainRecord.HostedAt = hostedAt;

            await _domainRepository.SaveChangesAsync(cancellationToken);

            return MapToResponse(domainRecord, source: "external", dnsNameServers: dnsResponse.NameServers);
        }

        private bool IsExpired(DomainRecord domainRecord)
        {
            if (domainRecord.Ttl <= 0)
            {
                return true;
            }

            var normalizedUpdatedAt = NormalizeDateTime(domainRecord.UpdatedAt);
            var elapsedSeconds = (_clock.UtcNow - normalizedUpdatedAt).TotalSeconds;
            return elapsedSeconds >= domainRecord.Ttl;
        }

        private static DomainLookupResponseDto MapToResponse(
            DomainRecord domainRecord,
            string source,
            IReadOnlyCollection<string> dnsNameServers)
        {
            return new DomainLookupResponseDto
            {
                Domain = domainRecord.Name ?? string.Empty,
                Ip = domainRecord.Ip ?? string.Empty,
                HostedAt = domainRecord.HostedAt ?? string.Empty,
                Whois = domainRecord.WhoIs ?? string.Empty,
                NameServers = BuildNameServers(dnsNameServers, domainRecord.WhoIs),
                Source = source
            };
        }

        private static DateTime NormalizeDateTime(DateTime dateTime)
        {
            return dateTime.Kind switch
            {
                DateTimeKind.Utc => dateTime,
                DateTimeKind.Local => dateTime.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
            };
        }

        private static IReadOnlyCollection<string> BuildNameServers(
            IReadOnlyCollection<string> dnsNameServers,
            string whoisRaw)
        {
            var uniqueNameServers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var server in dnsNameServers)
            {
                AddNameServer(uniqueNameServers, server);
            }

            if (!string.IsNullOrWhiteSpace(whoisRaw))
            {
                var matches = WhoisNameServerRegex.Matches(whoisRaw);
                foreach (Match match in matches)
                {
                    var value = match.Groups["value"].Value;
                    AddNameServer(uniqueNameServers, value);
                }
            }

            return uniqueNameServers.ToArray();
        }

        private static void AddNameServer(ISet<string> items, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            items.Add(value.Trim().TrimEnd('.').ToLowerInvariant());
        }
    }
}
