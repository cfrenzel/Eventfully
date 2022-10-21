using System;

namespace SimulationWorker
{
    
        public static class Key
        {
            public static Guid NewId()
            {
                return MassTransit.NewId.NextGuid();
            }
        }
    
}