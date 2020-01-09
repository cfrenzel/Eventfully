using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Eventfully.Transports;

namespace Eventfully
{
    public interface IEndpoint
    {
        List<string> BoundMessageIdentifiers { get; }
        List<Type> BoundMessageTypes { get; }
        bool IsReader { get; }
        bool IsWriter { get; }
        string Name { get; }
        EndpointSettings Settings { get; }
        bool SupportsDelayedDispatch { get; }
        Transport Transport { get; }

        Task Dispatch(string messageTypeIdenfifier, byte[] message, MessageMetaData metaData = null);
        void SetReplyToForCommand(IIntegrationCommand command, MessageMetaData meta);
        Task Start(CancellationToken cancellationToken = default);
    }
}