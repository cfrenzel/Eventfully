/*
using Eventfully.Filters;
using Eventfully.Transports;
using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{

    
    
    
    public class EndpointSettings :  ISupportFiltersFluent<EndpointSettings>
    {
        public string Name { get; set; }
        public string ConnectionString { get; set; }

        public bool IsReader { get; set; }
        public bool IsWriter { get; set; }
        public bool IsEventDefault { get; set; }
        public bool IsReplyDefault { get; set; }

        public List<Type> MessageTypes { get; set; } = new List<Type>();
        public List<string> MessageTypeIdentifiers { get; set; } = new List<string>();
        public TransportSettings TransportSettings { get; set; }

        public List<MessageSettings> MessageSettings = new List<MessageSettings>();

        internal readonly List<ITransportMessageFilter> InboundTransportFilters = new List<ITransportMessageFilter>();
        internal readonly List<IMessageFilter> InboundMessageFilters = new List<IMessageFilter>();
        internal readonly List<ITransportMessageFilter> OutboundTransportFilters = new List<ITransportMessageFilter>();
        internal readonly List<IMessageFilter> OutboundMessageFilters = new List<IMessageFilter>();
        
        public EndpointSettings(string name)//, string connectionString)
        {
            this.Name = name;
        }

       
        public virtual EndpointSettings BindEvent<T>(Action<MessageSettings> configBuilder = null) //where T : IEvent
        {
            var settings = new MessageSettings(typeof(T));
            this.MessageSettings.Add((settings));
            this.MessageTypes.Add(typeof(T));
            if(configBuilder != null)
                configBuilder.Invoke(settings);
            return this;
        }

        public virtual EndpointSettings BindCommand<T>(Action<MessageSettings> configBuilder = null) //where T : ICommand
        {
            var settings = new MessageSettings(typeof(T));
            this.MessageSettings.Add((settings));
            this.MessageTypes.Add(typeof(T));
            if(configBuilder != null)
                configBuilder.Invoke(settings);
            return this;
         }

        public virtual EndpointSettings BindCommand(string messageTypeIdentifier)
        {
            this.MessageTypeIdentifiers.Add(messageTypeIdentifier);
            return this;
        }

        /*public  EndpointSettings UseAesEncryption(string key, bool isBase64Encoded = false)
        {
            if (String.IsNullOrEmpty(key))
                throw new InvalidOperationException("UseEncryption requires a non empty value for key");

            return UseAesEncryption(null, new StringKeyProvider(key, isBase64Encoded));
        }

        public EndpointSettings UseAesEncryption(IEncryptionKeyProvider keyProvider)
        {
            return UseAesEncryption(null, keyProvider);
        }

        public EndpointSettings UseAesEncryption(string keyName, IEncryptionKeyProvider keyProvider)
        {
            this.WithFilter(new LevelTypeTransportFilter<T>(
                new AesEncryptionTransportFilter(keyName, keyProvider)
            ));
            return this;
        }#1#

        public EndpointSettings WithFilter(params ITransportMessageFilter[] filters)
        {
            AddFilter(filters);
            return this;
        }

        public EndpointSettings WithFilter(params IMessageFilter[] filters)
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

        internal void AddFilter(params IMessageFilter[] filters)
        {
            foreach (var filter in filters)
            {
                if (filter != null)
                {
                    if ((filter.SupportsDirection & FilterDirection.Inbound) == FilterDirection.Inbound)
                        this.InboundMessageFilters.Add(filter);
                    if ((filter.SupportsDirection & FilterDirection.Outbound) == FilterDirection.Outbound)
                        this.OutboundMessageFilters.Add(filter);
                }
            }
        }




        /*public EndpointSettings AsEventDefault()
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
        }#1#
    }

    /*public abstract class EndpointSubSettings : IEndpointFluent
    {
        protected readonly IEndpointFluent _settings;

        public EndpointSubSettings(IEndpointFluent settings)
        {
            _settings = settings;
        }

        public TransportSettings TransportSettings { get => _settings.TransportSettings; set => _settings.TransportSettings = value; }

        /*public EndpointSettings AsEventDefault()
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
        }#2#

        public BindMessageSettings<T> BindCommand<T>() where T : ICommand
        {
            return _settings.BindCommand<T>();
        }

        public EndpointSettings BindCommand(string messageTypeIdentifier)
        {
            return _settings.BindCommand(messageTypeIdentifier);
        }

        public BindMessageSettings<T> BindEvent<T>() where T : IEvent
        {
            return _settings.BindEvent<T>();
        }


        public EndpointSettings WithFilter(params IMessageLevelFilter[] filters)
        {
            return _settings.WithFilter(filters);
        }

        public EndpointSettings WithFilter(params ITransportLevelFilter[] filters)
        {
            return _settings.WithFilter(filters);
        }

    }#1#


    /*public class BindMessageSettings<T> : EndpointSubSettings
        where T : IMessage
    {

        public BindMessageSettings(EndpointSettings settings) : base(settings)
        {
        }

        public BindMessageSettings<T> UseAesEncryption(string key, bool isBase64Encoded = false)
        {
            if (String.IsNullOrEmpty(key))
                throw new InvalidOperationException("UseEncryption requires a non empty value for key");

            return UseAesEncryption(null, new StringKeyProvider(key, isBase64Encoded));
        }

        public BindMessageSettings<T> UseAesEncryption(IEncryptionKeyProvider keyProvider)
        {
            return UseAesEncryption(null, keyProvider);
        }

        public BindMessageSettings<T> UseAesEncryption(string keyName, IEncryptionKeyProvider keyProvider)
        {
            _settings.WithFilter(new LevelTypeTransportFilter<T>(
                new AesEncryptionTransportFilter(keyName, keyProvider)
            ));
            return this;
        }
    }#1#


   

}
*/
