using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.Outboxing
{
    public class OutboxRelayResult
    {
        public int MessageCount { get; set; }
        public int MaxMessageCount { get; set; }

        public OutboxRelayResult(int messageCount, int maxMessageCount)
        {
            this.MessageCount = messageCount;
            this.MaxMessageCount = maxMessageCount;
        }

    }
}
