using System;
using System.Collections.Generic;
using System.Text;
using Eventfully.Filters;
using Eventfully.Transports;

namespace Eventfully
{
    public interface IEndpointFluent
    {
        TransportSettings TransportSettings { get; set; }

        EndpointSettings AsEventDefault();
        EndpointSettings AsInbound();
        EndpointSettings AsInboundOutbound();
        EndpointSettings AsOutbound();
        EndpointSettings AsReplyDefault();

        BindMessageSettings<T> BindCommand<T>() where T : IIntegrationCommand;
        BindMessageSettings<T> BindEvent<T>() where T : IIntegrationEvent;
        EndpointSettings BindCommand(string messageTypeIdentifier);

        EndpointSettings WithFilter(params IIntegrationMessageFilter[] filters);
        EndpointSettings WithFilter(params ITransportMessageFilter[] filters);

        //EndpointSettings WithOutboundFilter(params IIntegrationMessageFilter[] filters);
        //EndpointSettings WithOutboundFilter(params ITransportMessageFilter[] filters);
    }

}
