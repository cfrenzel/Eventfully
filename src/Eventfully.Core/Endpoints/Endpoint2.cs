using Eventfully.Transports;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;using Eventfully;
using Eventfully.Filters;

namespace Eventfully
{
    
    public class EndpointSettings<T> : ISupportFiltersFluent<T> where T : EndpointSettings<T>
    {
        public string Name { get; set; }

        public List<Type> MessageTypes { get; set; } = new List<Type>();
        public List<string> MessageTypeIdentifiers { get; set; } = new List<string>();

        public List<MessageSettings> MessageSettings = new List<MessageSettings>();

        public readonly List<ITransportMessageFilter> TransportMessageFilters = new List<ITransportMessageFilter>();
        public readonly List<IMessageFilter> MessageFilters = new List<IMessageFilter>();

        internal void AddFilter(params ITransportMessageFilter[] filters)
        {
            foreach (var filter in filters)
            {
                if (filter != null)
                    this.TransportMessageFilters.Add(filter);
            }
        }

        internal void AddFilter(params IMessageFilter[] filters)
        {
            foreach (var filter in filters)
            {
                if (filter != null)
                    this.MessageFilters.Add(filter);
            }
        }
        T ISupportFiltersFluent<T>.WithFilter(params ITransportMessageFilter[] filters)
        {
            AddFilter(filters);
            return this as T;
        }

        T ISupportFiltersFluent<T>.WithFilter(params IMessageFilter[] filters)
        {
            AddFilter(filters);
            return this as T;
        }
    }
    
    
    
    public interface IEndpoint2
    { 
        string Name { get; }
        bool SupportsDelayedDispatch { get; }
        HashSet<string> MessageIdentifiers { get; }
        HashSet<Type> MessageTypes { get; }
        Task Dispatch(string messageTypeIdenfifier, byte[] message, MessageMetaData metaData = null);
        //void SetReplyToForCommand(ICommand command, MessageMetaData meta);
        MessageFilterContext TempInboundProcess(TransportMessageFilterContext transportContext);
        TransportMessageFilterContext TempOutboundProcess(MessageFilterContext messageContext);

    }
    
    public abstract class Endpoint2<T> : IEndpoint2 where T : EndpointSettings<T>
    {
        //protected readonly EndpointSettings<T> _settings;
        protected readonly MessagePipeline _pipeline;
        public string Name { get; protected set; }
        protected readonly  T _settings;

        //public EndpointSettings Settings => _settings;
        //public ITransport Transport => _transport;

        
        public HashSet<Type> MessageTypes { get; protected set; } = new HashSet<Type>();
        public HashSet<string> MessageIdentifiers { get; protected set; } = new HashSet<string>();

        public abstract bool SupportsDelayedDispatch { get; }
        
        public Endpoint2(EndpointSettings<T> settings)
        {
            _settings = settings as T;
            _pipeline = new MessagePipeline(settings.TransportMessageFilters, settings.MessageFilters);
            
            this.Name = settings.Name;
            
            // if (settings.MessageTypes != null)
            // {
            //     foreach (Type messageType in settings.MessageTypes)
            //         this.MessageTypes.Add(messageType);
            // }
            //
            // if (settings.MessageTypeIdentifiers != null)
            // {
            //     foreach (string messageTypeIdentifier in settings.MessageTypeIdentifiers)
            //         this.MessageIdentifiers.Add(messageTypeIdentifier);
            // }
        }

        public MessageFilterContext TempInboundProcess(TransportMessageFilterContext transportContext)
        {
            return _pipeline.OnIncoming(transportContext);
        }
        public TransportMessageFilterContext TempOutboundProcess( MessageFilterContext messageContext)
        {
            return _pipeline.OnOutgoing(messageContext);
        }
        
        public abstract Task Dispatch(string messageTypeIdenfifier, byte[] message, MessageMetaData metaData = null);
        
        //public virtual void SetReplyToForCommand(ICommand command, MessageMetaData meta)
        //{
        //    Transport.SetReplyToForCommand(this, command, meta);
        //}


    }
}
