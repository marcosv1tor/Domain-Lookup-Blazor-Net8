using System;
using System.Net;

namespace Desafio.Umbler.Web.Clients
{
    public sealed class DomainApiClientException : Exception
    {
        public DomainApiClientException(HttpStatusCode statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }
    }
}
