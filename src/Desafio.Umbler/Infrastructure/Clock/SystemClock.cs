using System;
using Desafio.Umbler.Application.Contracts;

namespace Desafio.Umbler.Infrastructure.Clock
{
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
