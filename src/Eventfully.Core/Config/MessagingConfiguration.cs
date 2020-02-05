using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{
    public class MessagingConfiguration
    {
        public ICountingSemaphore OutboxConsumerSemaphore { get; set; }
    }
}
