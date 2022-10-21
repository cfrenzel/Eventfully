using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Eventfully.Samples.ConsoleApp
{
    public class OrderSubmitted: Event 
    {
        public override string MessageType => "Ordering.OrderSubmitted";

        public Guid OrderId { get; private set; }
        public Decimal TotalDue { get; private set; }
        public string CurrencyCode { get; private set; }
        public Address ShippingAddress { get; private set; }

        private OrderSubmitted(){}

        public OrderSubmitted(Guid orderId, Decimal totalDue, string currencyCode,  Address shippignAddr)
        {
            this.OrderId = orderId;
            this.TotalDue = totalDue;
            this.CurrencyCode = currencyCode;
            this.ShippingAddress = shippignAddr;
        }

        public class Address
        {
            public string Line1 { get; private set; }
            public string Line2 { get; private set; }
            public string City { get; private set; }
            public string StateCode { get; private set; }
            public string Zip { get; private set; }

            private Address(){}
            public Address(string line1, string line2, string city, string state, string zip)
            {
                this.Line1 = line1;
                this.Line2 = line2;
                this.City = city;
                this.StateCode = state;
                this.Zip = zip;
            }

        }
    
    }
    
    public class SubmittedHandler : IMessageHandler<OrderSubmitted>
    {
        public Task Handle(OrderSubmitted ev, MessageContext context)
        {
            Console.WriteLine($"Received OrderSubmitted Event");
            Console.WriteLine($"\tOrderId: {ev.OrderId}");
            Console.WriteLine($"\tTotal Due: {ev.TotalDue} {ev.CurrencyCode}");
            Console.WriteLine($"\tShipping Address:");
            Console.WriteLine($"\t\t{ev.ShippingAddress.Line1}");
            if(!String.IsNullOrEmpty(ev.ShippingAddress.Line2))
                Console.WriteLine($"\t\t{ev.ShippingAddress.Line2}");
            Console.WriteLine($"\t\t{ev.ShippingAddress.City}, {ev.ShippingAddress.StateCode} {ev.ShippingAddress.Zip}");
            Console.WriteLine();

            return Task.CompletedTask;
        }
    }
}
