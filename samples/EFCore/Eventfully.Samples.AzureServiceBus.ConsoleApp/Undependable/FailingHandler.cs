using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Eventfully.Samples.ConsoleApp
{
    public class FailingHandler
    {

        public class Event : Eventfully.Event
        {
            public override string MessageType => "FailingHandler.Created";
            public DateTime FailUntilUtc { get; set; }
            public Guid Id { get; set; } = Guid.NewGuid();

            private Event() { }
            public Event(DateTime failUntilUtc) => FailUntilUtc = failUntilUtc;

        }

        public class Handler : IMessageHandler<Event>
        {
            public Task Handle(Event ev, MessageContext context)
            {
                Console.WriteLine($"Received FailingHandler.Created Event");
                Console.WriteLine($"\tId: {ev.Id}");
                if (DateTime.UtcNow < ev.FailUntilUtc)
                {
                    Console.WriteLine($"\tFailing Until: {ev.FailUntilUtc} - {(ev.FailUntilUtc - DateTime.UtcNow).TotalMinutes :N1} minutes remaining");
                    Console.WriteLine();
                    throw new ApplicationException($"Failing until {ev.FailUntilUtc}");
                }
                Console.WriteLine($"\tSucceeded After: {ev.FailUntilUtc}");
                Console.WriteLine();
                return Task.CompletedTask;
            }
        }
    }
}
