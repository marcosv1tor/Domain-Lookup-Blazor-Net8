using System;

namespace Desafio.Umbler.Application.Exceptions
{
    public sealed class DomainValidationException : Exception
    {
        public DomainValidationException(string message)
            : base(message)
        {
        }
    }
}
