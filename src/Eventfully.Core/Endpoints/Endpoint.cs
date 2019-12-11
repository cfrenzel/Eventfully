using Eventfully.Transports;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eventfully
{

    public class Endpoint
    {
        private readonly EndpointSettings _settings;
        private readonly Transport _transport;
        
        public string Name { get; private set; }

        public EndpointSettings Settings  => _settings;
        public Transport Transport  => _transport;

        public List<Type> BoundMessageTypes { get; protected set; } = new List<Type>();
        public List<string> BoundMessageIdentifiers { get; protected set; } = new List<string>();

        public bool IsReader { get; protected set; }
        public bool IsWriter { get; protected set; }

        public bool SupportsDelayedDispatch => _transport.SupportsDelayedDispatch;
        public Endpoint(EndpointSettings settings)
        {
            _settings = settings;
            this.Name = settings.Name;
            this.IsReader = settings.IsReader;
            this.IsWriter = settings.IsWriter;

            foreach (Type messageType in settings.MessageTypes)
                this.BoundMessageTypes.Add(messageType);

            foreach (string messageTypeIdentifier in settings.MessageTypeIdentifiers)
                this.BoundMessageIdentifiers.Add(messageTypeIdentifier);

            //create the transport from the supplied factory
            _transport = settings.TransportSettings.Create();
        }

        public Task Start(CancellationToken cancellationToken = default(CancellationToken))
        {
            return _transport.Start(this, cancellationToken);
        }

        public Task Dispatch(string messageTypeIdenfifier, byte[] message, MessageMetaData metaData = null)
        {
            return Transport.Dispatch(messageTypeIdenfifier, message, this, metaData);
        }

        public void SetReplyToForCommand(IIntegrationCommand command, MessageMetaData meta)
        {
            Transport.SetReplyToForCommand(this, command, meta);
        }


    }  
}
