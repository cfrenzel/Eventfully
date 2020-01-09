using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.EFCoreOutbox
{
    public class ConstantRetryStrategy : IRetryIntervalStrategy
    {
        private readonly double _intervalInSeconds;
        public ConstantRetryStrategy(double intervalInSeconds)
        {
            if (intervalInSeconds <= 0)
                throw new InvalidOperationException("interval must be greater than 0");
            _intervalInSeconds = intervalInSeconds;
        }
        public DateTime GetNextDateUtc(int tryCount, DateTime? nowUtc = null)
        {
            var counter = tryCount > 0 ? tryCount : 1;
            nowUtc = nowUtc.HasValue ? nowUtc : DateTime.UtcNow;

            return nowUtc.Value.AddSeconds(_intervalInSeconds);
        }
    }
}
