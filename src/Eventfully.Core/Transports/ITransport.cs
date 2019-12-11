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

        Task Start(Endpoint endpoint, CancellationToken cancellationToken);

        Task Dispatch(string messageTypeIdenfifier, byte[] message, Endpoint endpoint, MessageMetaData metaData = null);

        void SetReplyToForCommand(Endpoint endpoint, IIntegrationCommand command, MessageMetaData meta);


    }
}
