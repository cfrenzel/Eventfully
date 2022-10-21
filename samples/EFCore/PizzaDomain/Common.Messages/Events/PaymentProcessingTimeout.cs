using System;

namespace Contracts.Events
{

    public class PaymentRequestTimeout
    {
        public Guid OrderId { get; set; }
        public string IdempotencyKey { get; set; }
    }
}