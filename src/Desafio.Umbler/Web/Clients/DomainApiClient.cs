using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Desafio.Umbler.ClientModels;
using Microsoft.AspNetCore.Components;

namespace Desafio.Umbler.Web.Clients
{
    public sealed class DomainApiClient : IDomainApiClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly NavigationManager _navigationManager;

        public DomainApiClient(HttpClient httpClient, NavigationManager navigationManager)
        {
            _httpClient = httpClient;
            _navigationManager = navigationManager;
        }

        public async Task<DomainLookupVm> GetAsync(string domain, CancellationToken cancellationToken = default)
        {
            var requestUri = new Uri(new Uri(_navigationManager.BaseUri), $"api/domain/{Uri.EscapeDataString(domain)}");

            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                await using var successStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var result = await JsonSerializer.DeserializeAsync<DomainLookupVm>(successStream, JsonOptions, cancellationToken);
                if (result is not null)
                {
                    return result;
                }

                throw new DomainApiClientException(response.StatusCode, "Response payload is empty.");
            }

            ProblemDetailsVm? problem = null;
            var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (mediaType.Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await using var errorStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    problem = await JsonSerializer.DeserializeAsync<ProblemDetailsVm>(errorStream, JsonOptions, cancellationToken);
                }
                catch (JsonException)
                {
                    problem = null;
                }
            }

            var message = ExtractErrorMessage(problem);
            throw new DomainApiClientException(response.StatusCode, message);
        }

        private static string ExtractErrorMessage(ProblemDetailsVm? problemDetails)
        {
            if (problemDetails is null)
            {
                return "Nao foi possivel concluir a consulta. Tente novamente.";
            }

            if (!string.IsNullOrWhiteSpace(problemDetails.Detail))
            {
                return problemDetails.Detail;
            }

            if (!string.IsNullOrWhiteSpace(problemDetails.Title))
            {
                return problemDetails.Title;
            }

            var firstError = (problemDetails.Errors ?? new Dictionary<string, string[]>())
                .SelectMany(kvp => kvp.Value ?? Array.Empty<string>())
                .FirstOrDefault(error => !string.IsNullOrWhiteSpace(error));

            return firstError ?? "Nao foi possivel concluir a consulta. Tente novamente.";
        }
    }
}
