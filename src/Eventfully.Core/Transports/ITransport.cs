using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eventfully.Transports
{
    public interface ITransport
    {
        bool SupportsDelayedDispatch { get; }

        //Task StartAsync(IEndpoint endpoint, CancellationToken cancellationToken);
        Task StartAsync(IEndpoint endpoint, Handler handler, CancellationToken cancellationToken);

        Task StopAsync();

        Task Dispatch(string messageTypeIdenfifier, byte[] message, IEndpoint endpoint, MessageMetaData metaData = null);

        void SetReplyToForCommand(IEndpoint endpoint, IIntegrationCommand command, MessageMetaData meta);

        IEndpoint FindEndpointForReply(MessageContext commandContext);

    }
}
