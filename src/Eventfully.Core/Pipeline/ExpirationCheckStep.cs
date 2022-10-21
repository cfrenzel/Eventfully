using System;
using System.Collections.Generic;

namespace Eventfully.Filters
{
    public class ExpirationCheckStep : IBidirectionalPipelineStep<MessageFilterContext>
    {
        public MessageFilterContext OnIncoming(MessageFilterContext context)
        {
            if (context.Message != null)
            {
                if (context.MessageMetaData != null && context.MessageMetaData.IsExpired())
                    throw new MessageExpiredException(context.Message.MessageType);
            }
            return context;
        }

        public MessageFilterContext OnOutgoing(MessageFilterContext input)
        {
            return input;
        }
    }
}
