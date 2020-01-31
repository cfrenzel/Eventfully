using Eventfully.Filters;
using Eventfully.Transports;
using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{
    public interface IEndpointFluent
    {
        TransportSettings TransportSettings { get; set; }

        EndpointSettings AsEventDefault();
        EndpointSettings AsInbound();
        EndpointSettings AsInboundOutbound();
        EndpointSettings AsOutbound();
        EndpointSettings AsReplyDefault();

        BindMessageSettings<T> BindCommand<T>() where T : IIntegrationCommand;
        BindMessageSettings<T> BindEvent<T>() where T : IIntegrationEvent;
        EndpointSettings BindCommand(string messageTypeIdentifier);

        EndpointSettings WithFilter(params IIntegrationMessageFilter[] filters);
        EndpointSettings WithFilter(params ITransportMessageFilter[] filters);

        //EndpointSettings WithOutboundFilter(params IIntegrationMessageFilter[] filters);
        //EndpointSettings WithOutboundFilter(params ITransportMessageFilter[] filters);
    }

    public class EndpointSettings : IEndpointFluent
    {
        public string Name { get; set; }
        public string ConnectionString { get; set; }

        public bool IsReader { get;  set; }
        public bool IsWriter { get;  set; }
        public bool IsEventDefault { get;  set; }
        public bool IsReplyDefault { get; set; }

        public List<Type> MessageTypes { get;  set; } = new List<Type>();
        public List<string> MessageTypeIdentifiers { get;  set; } = new List<string>();


        public TransportSettings TransportSettings { get; set; }

        internal readonly List<ITransportMessageFilter> InboundTransportFilters = new List<ITransportMessageFilter>();
        internal readonly List<IIntegrationMessageFilter> InboundIntegrationFilters = new List<IIntegrationMessageFilter>();
        internal readonly List<ITransportMessageFilter> OutboundTransportFilters = new List<ITransportMessageFilter>();
        internal readonly List<IIntegrationMessageFilter> OutboundIntegrationFilters = new List<IIntegrationMessageFilter>();


        public EndpointSettings(string name) : this(name, null)
        {
        }

        public EndpointSettings(string name, string connectionString)
        {
            this.Name = name;
            this.ConnectionString = connectionString;
        }

        public EndpointSettings AsOutbound()
        {
            this.IsWriter = true;
            return this;
        }

        public EndpointSettings AsInbound()
        {
            this.IsReader = true;
            return this;
        }
        public EndpointSettings AsInboundOutbound()
        {
            this.IsReader = true;
            this.IsWriter = true;
            return this;
        }

        public virtual BindMessageSettings<T> BindEvent<T>() where T : IIntegrationEvent
        {
            this.MessageTypes.Add(typeof(T));
            return new BindMessageSettings<T>(this);
        }

        public virtual BindMessageSettings<T> BindCommand<T>() where T : IIntegrationCommand
        {
            this.MessageTypes.Add(typeof(T));
            return new BindMessageSettings<T>(this);
        }

        public virtual EndpointSettings BindCommand(string messageTypeIdentifier)
        {
            this.MessageTypeIdentifiers.Add(messageTypeIdentifier);
            return this;
        }


        public EndpointSettings WithFilter(params ITransportMessageFilter[] filters)
        {
            //if (!this.IsReader)
            //    throw new InvalidOperationException("Endpoint must be marked AsInbound to add InboundFilter");
            AddFilter(filters);
            return this;
        }

        public EndpointSettings WithFilter(params IIntegrationMessageFilter[] filters)
        {
            AddFilter(filters);
            return this;
        }

        internal void AddFilter(params ITransportMessageFilter[] filters)
        {
            foreach (var filter in filters)
            {
                if (filter != null)
                {
                    if ((filter.SupportsDirection & FilterDirection.Inbound) == FilterDirection.Inbound)
                        this.InboundTransportFilters.Add(filter);
                    if ((filter.SupportsDirection & FilterDirection.Outbound) == FilterDirection.Outbound)
                        this.OutboundTransportFilters.Add(filter);
                }
            }
        }

        internal void AddFilter(params IIntegrationMessageFilter[] filters)
        {
            foreach (var filter in filters)
            {
                if (filter != null)
                {
                    if ((filter.SupportsDirection & FilterDirection.Inbound) == FilterDirection.Inbound)
                        this.InboundIntegrationFilters.Add(filter);
                    if ((filter.SupportsDirection & FilterDirection.Outbound) == FilterDirection.Outbound)
                        this.OutboundIntegrationFilters.Add(filter);
                }
            }
        }




        public EndpointSettings AsEventDefault()
        {
            if (!this.IsWriter)
                throw new InvalidOperationException("Endpoint must be marked AsOutbound to be marked as EventDefault");

            this.IsEventDefault = true;
            return this;
        }
        public EndpointSettings AsReplyDefault()
        {
            if (!this.IsReader)
                throw new InvalidOperationException("Endpoint must be marked AsInbound to be marked as ReplyDefault");

            this.IsReplyDefault = true;
            return this;
        }
    }

    public abstract class EndpointSubSettings : IEndpointFluent
    {
        protected readonly EndpointSettings _settings;

        public EndpointSubSettings(EndpointSettings settings)
        {
            _settings = settings;
        }

        public TransportSettings TransportSettings { get => _settings.TransportSettings; set => _settings.TransportSettings = value; }

        public EndpointSettings AsEventDefault()
        {
            return _settings.AsEventDefault();
        }

        public EndpointSettings AsInbound()
        {
            return _settings.AsInbound();
        }

        public EndpointSettings AsInboundOutbound()
        {
            return _settings.AsInboundOutbound();
        }

        public EndpointSettings AsOutbound()
        {
            return _settings.AsOutbound();
        }

        public EndpointSettings AsReplyDefault()
        {
            return _settings.AsReplyDefault();
        }

        public BindMessageSettings<T> BindCommand<T>() where T : IIntegrationCommand
        {
            return _settings.BindCommand<T>();
        }

        public EndpointSettings BindCommand(string messageTypeIdentifier)
        {
            return _settings.BindCommand(messageTypeIdentifier);
        }

        public BindMessageSettings<T> BindEvent<T>() where T : IIntegrationEvent
        {
            return _settings.BindEvent<T>();
        }


        public EndpointSettings WithFilter(params IIntegrationMessageFilter[] filters)
        {
            return _settings.WithFilter(filters);
        }

        public EndpointSettings WithFilter(params ITransportMessageFilter[] filters)
        {
            return _settings.WithFilter(filters);
        }

    }


    public class BindMessageSettings<T> : EndpointSubSettings
        where T : IIntegrationMessage
    {

        public BindMessageSettings(EndpointSettings settings) : base(settings)
        {
        }

        public BindMessageSettings<T> UseAesEncryption(string key, bool isBase64Encoded = false)
        {
            if(String.IsNullOrEmpty(key))
                throw new InvalidOperationException("UseEncryption requires a non empty value for key");

            return UseAesEncryption(null, new StringKeyProvider(key, isBase64Encoded));
        }

        public BindMessageSettings<T> UseAesEncryption(IEncryptionKeyProvider keyProvider)
        {
            return UseAesEncryption(null, keyProvider);
        }

        public BindMessageSettings<T> UseAesEncryption(string keyName, IEncryptionKeyProvider keyProvider)
        {
            _settings.WithFilter(new MessageTypeTransportFilter<T>(
                new AesEncryptionTransportFilter(keyName, keyProvider)
            ));
            return this;
        }
    }



}
