using System;

namespace OrderingApi
{
    
        public static class Key
        {
            public static Guid NewId()
            {
                return MassTransit.NewId.NextSequentialGuid();
            }
        }
    
}