using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eventfully.Transports
{
    public abstract class Transport : ITransport
    {

        public abstract bool SupportsDelayedDispatch { get; }

        /// <summary>
        /// Start sending/receiving for the endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task Start(Endpoint endpoint, CancellationToken cancellationToken);

        /// <summary>
        /// Dispatch a pre-serialized message to the supplied endpoint
        /// </summary>
        /// <param name="messageTypeIdentifier"></param>
        /// <param name="message"></param>
        /// <param name="endpoint"></param>
        /// <param name="metaData"></param>
        /// <returns></returns>
        public abstract Task Dispatch(string messageTypeIdentifier, byte[] message, Endpoint endpoint, MessageMetaData metaData = null);
       
        /// <summary>
        /// Use the ReplyTo information in the meata data
        /// to provide an endpoint
        /// </summary>
        /// <param name="commandContext"></param>
        /// <returns></returns>
        public abstract Endpoint FindEndpointForReply(MessageContext commandContext);

        /// <summary>
        /// Populate the ReplyTo meta data for a command using
        /// the transports addressing/routing scheme
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="command"></param>
        /// <param name="meta"></param>
        public abstract void SetReplyToForCommand(Endpoint endpoint, IIntegrationCommand command, MessageMetaData meta);

    }
}
