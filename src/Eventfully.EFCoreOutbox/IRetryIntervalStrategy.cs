using System;

namespace Eventfully.EFCoreOutbox
{
    public interface IRetryIntervalStrategy
    {
        DateTime GetNextDateUtc(int tryCount, DateTime? nowUtc = null);
    }
}