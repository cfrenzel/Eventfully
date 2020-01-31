using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Eventfully.Transports;

namespace Eventfully
{
    public interface IEndpoint
    {
        HashSet<string> BoundMessageIdentifiers { get; }
        HashSet<Type> BoundMessageTypes { get; }
        bool IsReader { get; }
        bool IsWriter { get; }
        string Name { get; }
        EndpointSettings Settings { get; }
        bool SupportsDelayedDispatch { get; }
        ITransport Transport { get; }

        Task Dispatch(string messageTypeIdenfifier, byte[] message, MessageMetaData metaData = null);
        void SetReplyToForCommand(IIntegrationCommand command, MessageMetaData meta);
        Task StartAsync(Handler handler, CancellationToken cancellationToken = default);
        Task StopAsync();
    }
}