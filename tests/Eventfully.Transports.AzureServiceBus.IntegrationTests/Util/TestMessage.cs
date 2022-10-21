using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.Transports.AzureServieBus.IntegrationTests
{
    public class TestMessage : Event
    {
        public override string MessageType => "Test.AsbMessageType";
   
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime MessageDate { get; set; } = DateTime.UtcNow;

    }
}
