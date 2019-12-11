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

        internal Endpoint Endpoint { get;  set; }

        internal MessagingService MessagingService { get; set; }

        internal MessageTypeProperties Props { get; set; }

        internal IOutboxSession OutboxSession { get; set; }

        public MessageContext(MessageMetaData meta, Endpoint endpoint, MessagingService messagingService, MessageTypeProperties props = null)
        {
           MetaData = meta;
           Endpoint = endpoint;
           MessagingService = messagingService;
           Props = props;
        }

        public Task Reply(IIntegrationReply reply, MessageMetaData replyMetaData = null)
        {
            var replyToEndpoint = this.GetEndpointForReply();
            return MessagingService.Reply(reply, replyToEndpoint, this.OutboxSession, replyMetaData, this.MetaData);
        }

        internal Endpoint GetEndpointForReply()
        {
            return Endpoint.Transport.FindEndpointForReply(this);
        }

    }
}
