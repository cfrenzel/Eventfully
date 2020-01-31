using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{
    public class DefaultExponentialRetryStrategy : IRetryIntervalStrategy
    {
        public DateTime GetNextDateUtc(int tryCount, DateTime? nowUtc = null)
        {
            var counter = tryCount > 0 ? tryCount : 1;
            nowUtc = nowUtc.HasValue ? nowUtc : DateTime.UtcNow;

            //2^3,2^4, 2^5... (8,16,32,64,128 (2min), 256 (4min), 512 (8min), 1024 (17min), 2048 (34min), 4096 (1hr) , 8192 ( 2.2hr) ....)
            return nowUtc.Value.AddSeconds(Math.Pow(2, counter + 2));
        }
    }
}
