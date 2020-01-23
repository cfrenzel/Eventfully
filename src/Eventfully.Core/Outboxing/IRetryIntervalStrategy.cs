using System;

namespace Eventfully
{
    public interface IRetryIntervalStrategy
    {
        DateTime GetNextDateUtc(int tryCount, DateTime? nowUtc = null);
    }
}