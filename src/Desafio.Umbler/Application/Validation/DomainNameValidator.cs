using System;
using System.Text.RegularExpressions;

namespace Desafio.Umbler.Application.Validation
{
    public static class DomainNameValidator
    {
        private static readonly Regex DomainRegex = new(
            "^(?=.{1,253}$)(?!.*\\.\\.)(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool TryNormalize(string? input, out string normalizedDomain, out string validationError)
        {
            normalizedDomain = string.Empty;
            validationError = string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                validationError = "Domain is required.";
                return false;
            }

            normalizedDomain = input.Trim().ToLowerInvariant();

            if (!normalizedDomain.Contains('.', StringComparison.Ordinal))
            {
                validationError = "Domain must include a valid TLD (example: umbler.com).";
                return false;
            }

            if (!DomainRegex.IsMatch(normalizedDomain))
            {
                validationError = "Domain format is invalid.";
                return false;
            }

            if (Uri.CheckHostName(normalizedDomain) != UriHostNameType.Dns)
            {
                validationError = "Domain must be a valid DNS host name.";
                return false;
            }

            return true;
        }
    }
}
