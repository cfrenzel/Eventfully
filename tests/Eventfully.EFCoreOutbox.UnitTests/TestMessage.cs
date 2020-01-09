using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.EFCoreOutbox.UnitTests
{
    public class TestMessage : IntegrationCommand
    {
        public override string MessageType => "Test.MesageType";
   
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime MessageDate { get; set; } = DateTime.UtcNow;

    }
}
