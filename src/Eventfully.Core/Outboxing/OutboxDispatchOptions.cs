using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.Outboxing
{
    public class OutboxDispatchOptions
    {
        public bool SkipTransientDispatch { get; set; } = false;

        public TimeSpan? Delay { get; set; }

        public DateTime? ExpiresAtUtc { get; set; }

        public OutboxDispatchOptions()
        {
        }

    }
}
