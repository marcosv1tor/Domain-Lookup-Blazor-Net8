using System;
using System.Text.RegularExpressions;

namespace Desafio.Umbler.Web.Validation
{
    public static class DomainInputValidator
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
                validationError = "Informe um dominio para pesquisar.";
                return false;
            }

            var normalized = input.Trim().ToLowerInvariant();
            if (!normalized.Contains('.', StringComparison.Ordinal))
            {
                validationError = "O dominio deve conter TLD. Exemplo: umbler.com";
                return false;
            }

            if (!DomainRegex.IsMatch(normalized))
            {
                validationError = "Dominio invalido. Verifique e tente novamente.";
                return false;
            }

            if (Uri.CheckHostName(normalized) != UriHostNameType.Dns)
            {
                validationError = "Dominio invalido. Verifique e tente novamente.";
                return false;
            }

            normalizedDomain = normalized;
            return true;
        }
    }
}
