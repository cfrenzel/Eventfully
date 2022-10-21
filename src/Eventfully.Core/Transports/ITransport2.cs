using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eventfully.Transports
{
    public interface ITransport2
    {
        bool SupportsDelayedDispatch { get; }

        Task StartAsync(CancellationToken cancellationToken);//IEndpoint endpoint, Handler handler, CancellationToken cancellationToken);

        Task StopAsync();

        IEnumerable<IEndpoint2> GetEndpoints();

        //Task Dispatch(string messageTypeIdenfifier, byte[] message, IEndpoint endpoint, MessageMetaData metaData = null);

        //void SetReplyToForCommand(IEndpoint endpoint, ICommand command, MessageMetaData meta);

        //IEndpoint FindEndpointForReply(MessageContext commandContext);

    }
}
