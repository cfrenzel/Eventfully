using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Eventfully.Filters
{
    public class OutboundMessagePipeline : Pipeline<IntegrationMessageFilterContext, TransportMessageFilterContext>
    {
        public OutboundMessagePipeline(IEnumerable<IIntegrationMessageFilter> userIntegrationFilters, IEnumerable<ITransportMessageFilter> userTransportFilters)
        {
            Pipeline<IntegrationMessageFilterContext> userPrePipeline = null;
            if (userIntegrationFilters != null && userIntegrationFilters.Count() > 0)
                userPrePipeline = new Pipeline<IntegrationMessageFilterContext>(userIntegrationFilters);

            Pipeline<TransportMessageFilterContext> userPostPipeline = null;
            if (userTransportFilters != null && userTransportFilters.Count() > 0)
                userPostPipeline = new Pipeline<TransportMessageFilterContext>(userTransportFilters);

          
            this.Steps = input =>
            {
                return input
                    .Step(userPrePipeline)
                    .Step(new MessageSerializationStep())
                    .Step(userPostPipeline)
                    ;
            };
        }
    }


}
