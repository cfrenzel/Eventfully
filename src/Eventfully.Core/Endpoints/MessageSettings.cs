using Eventfully.Filters;
using Eventfully.Transports;
using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{
    
    
    public class MessageSettings : ISupportFiltersFluent<MessageSettings>
    {
        public Type MessageType { get; set; }

        public readonly List<ITransportMessageFilter> TransportMessageFilters = new List<ITransportMessageFilter>();
        public readonly List<IMessageFilter> MessageFilters = new List<IMessageFilter>();

        
        public MessageSettings(Type messageType)
        {
            this.MessageType = messageType;
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
        }*/
        public void AddFilter(params ITransportMessageFilter[] filters)
        {
            foreach (var filter in filters)
            {
                if (filter != null)
                    this.TransportMessageFilters.Add(filter);
            }
        }

        public void AddFilter(params IMessageFilter[] filters)
        {
            foreach (var filter in filters)
            {
                if (filter != null)
                    this.MessageFilters.Add(filter);
            }
        }
        MessageSettings ISupportFiltersFluent<MessageSettings>.WithFilter(params ITransportMessageFilter[] filters)
        {
            AddFilter(filters);
            return this;
        }

        MessageSettings ISupportFiltersFluent<MessageSettings>.WithFilter(params IMessageFilter[] filters)
        {
            AddFilter(filters);
            return this;
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
        }*/
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
        }#1#

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

    }*/

}
