using System;
using System.Collections.Generic;
using System.Linq;

namespace Eventfully.Filters
{
    public class InboundMessagePipeline : Pipeline<TransportMessageFilterContext, IntegrationMessageFilterContext>
    {
        public InboundMessagePipeline(IEnumerable<ITransportMessageFilter> userTransportFilters, IEnumerable<IIntegrationMessageFilter> userIntegrationFilters)
        {
            Pipeline<TransportMessageFilterContext> userPrePipeline = null;
            if (userTransportFilters != null && userTransportFilters.Count() > 0)
                userPrePipeline = new Pipeline<TransportMessageFilterContext>(userTransportFilters);

            Pipeline<IntegrationMessageFilterContext> userPostPipeline = null;
            if (userIntegrationFilters != null && userIntegrationFilters.Count() > 0)
                userPostPipeline = new Pipeline<IntegrationMessageFilterContext>(userIntegrationFilters);

            this.Steps = input =>
            {
                return input
                    .Step(userPrePipeline)
                    .Step(new MessageExtractionStep())
                    .Step(userPostPipeline)
                    .Step(new ExpirationCheckStep())
                    ;
            };
        }
    }



}


