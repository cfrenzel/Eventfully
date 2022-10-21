using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Eventfully.Outboxing;

namespace Eventfully
{
    public class MessageContext
    {
        public MessageMetaData MetaData { get; protected set; }

        internal IEndpoint2 Endpoint { get;  set; }

        internal MessagingService MessagingService { get; set; }

        internal MessageTypeProperties2 Props { get; set; }

        internal IOutboxSession OutboxSession { get; set; }

        public MessageContext(MessageMetaData meta, IEndpoint2 endpoint, MessagingService messagingService, MessageTypeProperties2 props = null)
        {
           MetaData = meta;
           Endpoint = endpoint;
           MessagingService = messagingService;
           Props = props;
        }

        // public Task Reply(IReply reply, MessageMetaData replyMetaData = null)
        // {
        //     var replyToEndpoint = this.GetEndpointForReply();
        //     return MessagingService.Reply(reply, replyToEndpoint, this.OutboxSession, replyMetaData, this.MetaData);
        // }

        // internal IEndpoint2 GetEndpointForReply()
        // {
        //     return Endpoint.Transport.FindEndpointForReply(this);
        // }

    }
}
