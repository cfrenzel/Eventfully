using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eventfully.Transports
{
  
    public class LocalTransportSettings : TransportSettings
    {
        private readonly ITransportFactory _factory = new LocalTransportFactory();

        public override ITransportFactory Factory => _factory;

        public LocalTransportSettings() { }

    }
    public class LocalTransportFactory : ITransportFactory
    {
        public Transport Create(TransportSettings settings)
        {
            LocalTransport transport = new LocalTransport(settings);
            return transport;
        }
    }

    public class LocalTransport : Transport
    {
        private static readonly Dictionary<string, IEndpoint> _replyToEndpointCache = new Dictionary<string, IEndpoint>();
        private static readonly Dictionary<string, string> _replyToRouteCache = new Dictionary<string, string>();
        private static readonly Dictionary<string, IEndpoint> _endpointsByName = new Dictionary<string, IEndpoint>();

        private readonly TransportSettings _settings;

        public override bool SupportsDelayedDispatch => false;

        public LocalTransport(TransportSettings settings)
        {
            _settings = settings;
        }

        ///instead of sending the message out to a service bus, just return it to the messaging service for handling
        ///we'll let the outbox handle all the retries/delays/etc...
        public override Task Dispatch(string messageTypeIdentifier, byte[] message, IEndpoint endpoint, MessageMetaData metaData = null)
        {
            metaData = metaData ?? new MessageMetaData();
 
            //set the message type in the meta data
            metaData.MessageType = messageTypeIdentifier;
            return MessagingService.Instance.Handle(new TransportMessage(messageTypeIdentifier, message, metaData), endpoint);
        }

        public override IEndpoint FindEndpointForReply(MessageContext commandContext)
        {
            var replyTo = commandContext.MetaData != null ? commandContext.MetaData.ReplyTo : null;
            if (String.IsNullOrEmpty(replyTo))
                throw new InvalidOperationException("LocalTransport commands must set ReplyTo");

            if (_replyToEndpointCache.ContainsKey(replyTo))
                return _replyToEndpointCache[replyTo];

            if (!replyTo.StartsWith("local://endpoint="))
                throw new ApplicationException("Invalid replyTo for LocalTransport. Should start with local://endpoint=");
            else
                replyTo = replyTo.Substring(17);

            IEndpoint endpoint = null;
            if (_endpointsByName.TryGetValue(replyTo.ToLower(), out endpoint))
            {
                _replyToEndpointCache.Add(replyTo, endpoint);
                return endpoint;
            }
            return null;
        }

        public override void SetReplyToForCommand(IEndpoint endpoint, IIntegrationCommand command, MessageMetaData meta)
        {
            meta = meta ?? new MessageMetaData();
         
            if (!_replyToRouteCache.ContainsKey(endpoint.Name))
                _replyToRouteCache[endpoint.Name] = $"local://endpoint={endpoint.Name}";
            meta.ReplyTo = _replyToRouteCache[endpoint.Name];
        }

        public override Task Start(IEndpoint endpoint, CancellationToken cancellationToken)
        {
            _endpointsByName.Add(endpoint.Name.ToLower(), endpoint);
            return Task.CompletedTask;
        }
    }
}
