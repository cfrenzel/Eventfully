using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Eventfully.Samples.ConsoleApp
{
    public class PaymentMethodCreatedHandler : IMessageHandler<PaymentMethodCreated>
    {
        public Task Handle(PaymentMethodCreated ev, MessageContext context)
        {
            Console.WriteLine($"Received PaymentMethodCreated Event");
            Console.WriteLine($"\tId: {ev.PaymentMethodId}");
            Console.WriteLine($"\tPayment Method Info:");
            Console.WriteLine($"\t\t Name: {ev.MethodInfo.NameOnCard}");
            Console.WriteLine($"\t\t Number: {ev.MethodInfo.Number}");
            Console.WriteLine($"\t\t Expires: {ev.MethodInfo.ExpirationMonth} / {ev.MethodInfo.ExpirationYear}");
            Console.WriteLine();

            return Task.CompletedTask;
        }
    }
}
