using System;

namespace Desafio.Umbler.Application.Contracts
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}
