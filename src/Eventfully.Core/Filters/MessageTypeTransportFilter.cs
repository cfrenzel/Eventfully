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
        public FilterDirection SupportsDirection => _innerFilter.SupportsDirection;

        public MessageTypeTransportFilter(string messageTypeIdentifier, ITransportMessageFilter innerFilter)
        {
            _messageTypeIdentifier = messageTypeIdentifier;
            _innerFilter = innerFilter;
        }

        public TransportMessageFilterContext Process(TransportMessageFilterContext context)
        {
            if (context.TransportMessage.MessageTypeIdentifier.Equals(_messageTypeIdentifier, StringComparison.OrdinalIgnoreCase))
                return _innerFilter.Process(context);
            return context;
        }

    }

    public class MessageTypeTransportFilter<T> : ITransportMessageFilter
    {
        private readonly ITransportMessageFilter _innerFilter;
        public FilterDirection SupportsDirection => _innerFilter.SupportsDirection;

        public MessageTypeTransportFilter(ITransportMessageFilter innerFilter)
        {
            _innerFilter = innerFilter;
        }

   
        public TransportMessageFilterContext Process(TransportMessageFilterContext context)
        {
            if (context.TransportMessage.MessageTypeIdentifier.Equals(
                    MessagingMap.GetMessageTypeIdentifier(typeof(T)),
                    StringComparison.OrdinalIgnoreCase))
               return _innerFilter.Process(context);

            return context;
        }

    }
}
