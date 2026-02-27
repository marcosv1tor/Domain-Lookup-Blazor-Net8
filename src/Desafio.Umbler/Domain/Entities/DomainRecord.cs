using System;
using System.ComponentModel.DataAnnotations;

namespace Desafio.Umbler.Domain.Entities
{
    public class DomainRecord
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Ip { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; }

        public string WhoIs { get; set; } = string.Empty;

        public int Ttl { get; set; }

        public string HostedAt { get; set; } = string.Empty;
    }
}
