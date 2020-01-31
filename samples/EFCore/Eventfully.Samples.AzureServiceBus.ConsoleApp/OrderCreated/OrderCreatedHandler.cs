using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Eventfully.Samples.ConsoleApp
{
    public class OrderCreatedHandler : IMessageHandler<OrderCreated>
    {
        public Task Handle(OrderCreated ev, MessageContext context)
        {
            Console.WriteLine($"Received OrderCreated Event");
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
