using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Desafio.Umbler.Web.Clients;
using Desafio.Umbler.Web.Validation;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Desafio.Umbler.Test
{
    [TestClass]
    public class BlazorClientTests
    {
        [TestMethod]
        public async Task DomainApiClient_ReturnsDto_WhenApiSucceeds()
        {
            var handler = new StubHttpMessageHandler((_, _) =>
            {
                const string payload = "{\"domain\":\"umbler.com\",\"ip\":\"177.55.66.99\",\"hostedAt\":\"Umbler\",\"whois\":\"raw\",\"nameServers\":[\"ns1.umbler.com\"],\"source\":\"external\"}";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                });
            });

            var client = new DomainApiClient(new HttpClient(handler), CreateNavigationManager());

            var result = await client.GetAsync("umbler.com");

            Assert.AreEqual("umbler.com", result.Domain);
            Assert.AreEqual("177.55.66.99", result.Ip);
            Assert.AreEqual(1, result.NameServers.Count);
        }

        [TestMethod]
        public async Task DomainApiClient_ThrowsValidationMessage_WhenApiReturnsBadRequest()
        {
            var handler = new StubHttpMessageHandler((_, _) =>
            {
                const string payload = "{\"title\":\"Validation error\",\"status\":400,\"detail\":\"Domain must include a valid TLD\"}";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/problem+json")
                });
            });

            var client = new DomainApiClient(new HttpClient(handler), CreateNavigationManager());

            var exception = await Assert.ThrowsExceptionAsync<DomainApiClientException>(() => client.GetAsync("umbler"));

            Assert.AreEqual(HttpStatusCode.BadRequest, exception.StatusCode);
            StringAssert.Contains(exception.Message, "Domain must include a valid TLD");
        }

        [TestMethod]
        public async Task DomainApiClient_ThrowsGenericMessage_WhenApiReturnsServerError()
        {
            var handler = new StubHttpMessageHandler((_, _) =>
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("server error", Encoding.UTF8, "text/plain")
                });
            });

            var client = new DomainApiClient(new HttpClient(handler), CreateNavigationManager());

            var exception = await Assert.ThrowsExceptionAsync<DomainApiClientException>(() => client.GetAsync("umbler.com"));

            Assert.AreEqual(HttpStatusCode.InternalServerError, exception.StatusCode);
            StringAssert.Contains(exception.Message, "Nao foi possivel concluir a consulta");
        }

        [TestMethod]
        public void DomainInputValidator_RejectsDomainWithoutTld()
        {
            var isValid = DomainInputValidator.TryNormalize("umbler", out var normalized, out var error);

            Assert.IsFalse(isValid);
            Assert.AreEqual(string.Empty, normalized);
            StringAssert.Contains(error, "TLD");
        }

        [TestMethod]
        public void DomainInputValidator_NormalizesValidDomain()
        {
            var isValid = DomainInputValidator.TryNormalize("  UMBLER.COM  ", out var normalized, out var error);

            Assert.IsTrue(isValid);
            Assert.AreEqual("umbler.com", normalized);
            Assert.AreEqual(string.Empty, error);
        }

        private static NavigationManager CreateNavigationManager()
        {
            var navigationManager = new TestNavigationManager();
            navigationManager.InitializeForTest("https://localhost:5001/", "https://localhost:5001/");
            return navigationManager;
        }

        private sealed class TestNavigationManager : NavigationManager
        {
            public void InitializeForTest(string baseUri, string uri)
            {
                Initialize(baseUri, uri);
            }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
            }
        }

        private sealed class StubHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

            public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return _handler(request, cancellationToken);
            }
        }
    }
}
