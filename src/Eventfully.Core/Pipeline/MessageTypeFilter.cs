using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.Filters
{
    /// <summary>
    /// Filter wrapper for IntegrationFilters that should only be applied to 
    /// Messages of a specific type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MessageTypeFilter<T> : IMessageFilter //where T : IMessage
    {
        private readonly IMessageFilter _innerFilter;
       
        public MessageTypeFilter(IMessageFilter innerFilter)
        {
            _innerFilter = innerFilter;
        }

     
        public MessageFilterContext OnIncoming(MessageFilterContext context)
        {
            if (context.Message is T)
                return _innerFilter.OnIncoming(context);
            return context;
        }

        public MessageFilterContext OnOutgoing(MessageFilterContext context)
        {
            if (context.Message is T)
                return _innerFilter.OnOutgoing(context);
            return context;
        }
    }


}
