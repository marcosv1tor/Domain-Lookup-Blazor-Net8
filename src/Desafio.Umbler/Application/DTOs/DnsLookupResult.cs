using System;
using System.Collections.Generic;

namespace Desafio.Umbler.Application.DTOs
{
    public sealed class DnsLookupResult
    {
        public string Ip { get; init; } = string.Empty;

        public int Ttl { get; init; }

        public IReadOnlyCollection<string> NameServers { get; init; } = Array.Empty<string>();
    }
}
