using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.Filters
{
    /// <summary>
    /// Filter wrapper for TransportFilters that should only be applied to 
    /// Messages of a specific type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MessageTypeTransportFilter : ITransportMessageFilter
    {
        private readonly string _messageTypeIdentifier;
        private readonly ITransportMessageFilter _innerFilter;
        
        public MessageTypeTransportFilter(string messageTypeIdentifier, ITransportMessageFilter innerFilter)
        {
            _messageTypeIdentifier = messageTypeIdentifier;
            _innerFilter = innerFilter;
        }

        public TransportMessageFilterContext OnIncoming(TransportMessageFilterContext context)
        {
            if (context.TransportMessage.MessageTypeIdentifier.Equals(_messageTypeIdentifier, StringComparison.OrdinalIgnoreCase))
                return _innerFilter.OnIncoming(context);
            return context;
        }

        public TransportMessageFilterContext OnOutgoing(TransportMessageFilterContext context)
        {
            if (context.TransportMessage.MessageTypeIdentifier.Equals(_messageTypeIdentifier, StringComparison.OrdinalIgnoreCase))
                return _innerFilter.OnOutgoing(context);
            return context;
        }
       
    }
}
