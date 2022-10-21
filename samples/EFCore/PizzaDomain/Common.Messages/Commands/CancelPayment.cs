using System;

namespace Contracts.Commands
{

    public class CancelPayment
    {
        public Guid OrderId { get; set; }
    }
}