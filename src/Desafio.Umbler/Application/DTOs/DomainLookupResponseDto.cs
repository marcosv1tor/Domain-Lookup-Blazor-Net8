using System;
using System.Collections.Generic;

namespace Desafio.Umbler.Application.DTOs
{
    public sealed class DomainLookupResponseDto
    {
        public string Domain { get; init; } = string.Empty;

        public string Ip { get; init; } = string.Empty;

        public string HostedAt { get; init; } = string.Empty;

        public string Whois { get; init; } = string.Empty;

        public IReadOnlyCollection<string> NameServers { get; init; } = Array.Empty<string>();

        public string Source { get; init; } = string.Empty;
    }
}
