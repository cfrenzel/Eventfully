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
    public class MessageTypeIntegrationFilter<T> : IIntegrationMessageFilter
         where T : IIntegrationMessage
    {
        private readonly IIntegrationMessageFilter _innerFilter;
        public FilterDirection SupportsDirection => _innerFilter.SupportsDirection;

        public MessageTypeIntegrationFilter(IIntegrationMessageFilter innerFilter)
        {
            _innerFilter = innerFilter;
        }

     
        public IntegrationMessageFilterContext Process(IntegrationMessageFilterContext context)
        {
            if (context.Message is T)
                return _innerFilter.Process(context);
            return context;
        }

    }


}
