using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.Outboxing
{
    public class OutboxDispatchResult
    {
        public int MessageCount { get; set; }
        public int MaxMessageCount { get; set; }

        public OutboxDispatchResult(int messageCount, int maxMessageCount)
        {
            this.MessageCount = messageCount;
            this.MaxMessageCount = maxMessageCount;
        }

    }
}
