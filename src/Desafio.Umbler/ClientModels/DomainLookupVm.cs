using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Desafio.Umbler.ClientModels
{
    public sealed class DomainLookupVm
    {
        [JsonPropertyName("domain")]
        public string Domain { get; set; } = string.Empty;

        [JsonPropertyName("ip")]
        public string Ip { get; set; } = string.Empty;

        [JsonPropertyName("hostedAt")]
        public string HostedAt { get; set; } = string.Empty;

        [JsonPropertyName("whois")]
        public string Whois { get; set; } = string.Empty;

        [JsonPropertyName("nameServers")]
        public IReadOnlyCollection<string> NameServers { get; set; } = Array.Empty<string>();

        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;
    }
}
