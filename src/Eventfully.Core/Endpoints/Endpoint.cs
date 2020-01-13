using Eventfully.Transports;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eventfully
{

    public class Endpoint : IEndpoint
    {
        protected readonly EndpointSettings _settings;
        protected readonly Transport _transport;

        public string Name { get; protected set; }

        public EndpointSettings Settings => _settings;
        public Transport Transport => _transport;

        public List<Type> BoundMessageTypes { get; protected set; } = new List<Type>();
        public List<string> BoundMessageIdentifiers { get; protected set; } = new List<string>();

        public bool IsReader { get; protected set; }
        public bool IsWriter { get; protected set; }

        public bool SupportsDelayedDispatch => _transport.SupportsDelayedDispatch;

        public Endpoint(EndpointSettings settings)
            :this(settings, null)
        {}
        public Endpoint(EndpointSettings settings, Transport transport)
        {
            _settings = settings;
            this.Name = settings.Name;
            this.IsReader = settings.IsReader;
            this.IsWriter = settings.IsWriter;

            if (settings.MessageTypes != null)
            {
                foreach (Type messageType in settings.MessageTypes)
                    this.BoundMessageTypes.Add(messageType);
            }
            
            if (settings.MessageTypeIdentifiers != null)
            {
                foreach (string messageTypeIdentifier in settings.MessageTypeIdentifiers)
                    this.BoundMessageIdentifiers.Add(messageTypeIdentifier);
            }

            //create the transport from the supplied factory
            if (transport != null)
                _transport = transport;
            
            if(_transport == null)
                _transport = settings.TransportSettings.Create();
        }

        public virtual Task Start(CancellationToken cancellationToken = default(CancellationToken))
        {
            return _transport.Start(this, cancellationToken);
        }

        public virtual Task Dispatch(string messageTypeIdenfifier, byte[] message, MessageMetaData metaData = null)
        {
            return Transport.Dispatch(messageTypeIdenfifier, message, this, metaData);
        }

        public virtual void SetReplyToForCommand(IIntegrationCommand command, MessageMetaData meta)
        {
            Transport.SetReplyToForCommand(this, command, meta);
        }


    }
}
