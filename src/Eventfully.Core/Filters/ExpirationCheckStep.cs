using System;
using System.Collections.Generic;

namespace Eventfully.Filters
{
    public class ExpirationCheckStep : IPipelineStep<IntegrationMessageFilterContext>
    {
        public IntegrationMessageFilterContext Process(IntegrationMessageFilterContext context)
        {
            if (context.Message != null)
            {
                if (context.MessageMetaData != null && context.MessageMetaData.IsExpired())
                    throw new MessageExpiredException(context.Message.MessageType);
            }
            return context;
        }
    }
}
