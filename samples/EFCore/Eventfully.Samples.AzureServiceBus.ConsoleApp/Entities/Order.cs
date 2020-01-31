using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Eventfully.Samples.ConsoleApp
{
    public class Order
    {
        public Guid Id { get; set; }

        [Required, StringLength(500)]
        public string Number { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public Decimal Total { get; set; }

        [Required, StringLength(3)]
        public string CurrencyCode { get; set; }
        
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
