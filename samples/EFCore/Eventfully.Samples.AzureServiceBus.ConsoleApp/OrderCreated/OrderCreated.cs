using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.Samples.ConsoleApp
{

    public class OrderCreated : IntegrationEvent
    {
        public override string MessageType => "Sales.OrderCreated";

        public Guid OrderId { get; private set; }
        public Decimal TotalDue { get; private set; }
        public string CurrencyCode { get; private set; }
        public Address ShippingAddress { get; private set; }

        private OrderCreated(){}

        public OrderCreated(Guid orderId, Decimal totalDue, string currencyCode,  Address shippignAddr)
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
}
