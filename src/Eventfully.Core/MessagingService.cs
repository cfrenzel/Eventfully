using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using Eventfully.Filters;
using Eventfully.Handlers;
using Eventfully.Outboxing;
using System.Diagnostics;

namespace Eventfully
{
    public delegate Task Dispatcher(string messageTypeIdentifier, byte[] message, MessageMetaData metaData, string endpointName, bool isTransient=false);
    public delegate Task Handler(TransportMessage transportMessage, IEndpoint2 endpoint);

    /// <summary>
    /// A Singleton service that acts as the middleware for all configuration / 
    /// message dispatching / handling / outbox management / etc.
    /// All messages pass through here.
    /// </summary>
    public class MessagingService
    {
    
        //private readonly Dictionary<IEndpoint, InboundMessagePipeline> _endpointInboundPipeline = new Dictionary<IEndpoint2, InboundMessagePipeline>();
        //private readonly Dictionary<IEndpoint2, OutboundMessagePipeline> _endpointOutboundPipeline = new Dictionary<IEndpoint2, OutboundMessagePipeline>();
        
        private readonly IMessageDispatcher _messageHandlerDispatcher;
        private readonly IOutboxManager _outboxManager;
        private readonly MessagingConfiguration _configuration;

        private int _maxImmediateRetryCount = 1;// deal with transient errors before doing a more sophisticated retry with backoff
        private readonly AsyncRetryPolicy _immediateHandleRetryPolicy;

        public readonly int ProcessId;
        public readonly string MachineName;
        public string MachineAndProcessId => $"{MachineName}-{ProcessId}";

        public MessagingService(IOutbox outbox, IServiceFactory factory, MessagingConfiguration configuration = null)
        {
            ProcessId =  Process.GetCurrentProcess().Id;
            MachineName = Environment.MachineName;
          
            _configuration  = configuration ?? new MessagingConfiguration();

            if (outbox == null)
                throw new InvalidOperationException("Outbox cannot be null. An Outbox is required to instantiate MessagingService");
            _outboxManager = new OutboxManager(outbox, this.DispatchCore, 1, _configuration.OutboxConsumerSemaphore, this.MachineAndProcessId);
         
            if (factory == null)
                throw new InvalidOperationException("IServiceFactory cannot be null. An IServiceFactory is required to instantiate MessagingService");
            _messageHandlerDispatcher = new MessageDispatcher(factory);

            _immediateHandleRetryPolicy = Policy
                .Handle<Exception>(x => !(x is NonTransientException))
                .RetryAsync(_maxImmediateRetryCount);
        }


        /// <summary>
        /// Handle an inbound message from a transport
        /// </summary>
        /// <param name="transportMessage">the raw message data and headers</param>
        /// <param name="endpoint">the endpoint the message came in on</param>
        /// <returns></returns>
        public Task Handle(TransportMessage transportMessage, IEndpoint2 endpoint)
        {
           
            var resultContext = endpoint.TempInboundProcess(new TransportMessageFilterContext(transportMessage, endpoint));
            if (resultContext.Message != null)
            {
                //immediate retry for transient failures
                return _immediateHandleRetryPolicy.ExecuteAsync(() =>
                     _messageHandlerDispatcher.Dispatch(
                        resultContext.Message,
                        new MessageContext(resultContext.MessageMetaData, endpoint, this, resultContext.Props)
                 ));
            }
            throw new ApplicationException($"Unable to handle message. MessageType: {transportMessage.MetaData?.MessageType}, Endpoint: {endpoint?.Name}");
        }


        /************ Message Dispatching ***********/

        public Task Send(ICommand command, IOutboxSession outbox, MessageMetaData metaData = null)
        {
            metaData = metaData ?? new MessageMetaData();
            var endpoint = MessagingMap2.FindEndpoint(command);
            _setReplyTo(endpoint, command, metaData);
            return Dispatch(command, metaData, outbox);
        }
        public Task Send(string messageTypeIdenfifier, byte[] command, IOutboxSession outbox, MessageMetaData metaData = null)
        {
            metaData = metaData ?? new MessageMetaData();
            var endpoint = MessagingMap2.FindEndpoint(messageTypeIdenfifier);
            _setReplyTo(endpoint, null, metaData);
            return Dispatch(new WrappedCommand(messageTypeIdenfifier, command), metaData, endpoint, outbox);
        }
        public Task SendSynchronously(ICommand command, MessageMetaData metaData = null)
        {
            metaData = metaData ?? new MessageMetaData();
            var endpoint = MessagingMap2.FindEndpoint(command);
            _setReplyTo(endpoint, command, metaData);
            return Dispatch(command, metaData);
        }
        public Task SendSynchronously(string messageTypeIdenfifier, byte[] command, MessageMetaData metaData = null)
        {
            metaData = metaData ?? new MessageMetaData();
            var endpoint = MessagingMap2.FindEndpoint(messageTypeIdenfifier);
            _setReplyTo(endpoint, null, metaData);
            return Dispatch(new WrappedCommand(messageTypeIdenfifier, command), metaData, endpoint);
        }
        
        public Task Publish(IEvent @event, IOutboxSession outbox, MessageMetaData metaData = null)
        {
            var endpoint = MessagingMap2.FindEndpoint(@event);
            return Dispatch(@event, metaData, outbox);
        }
        public Task Publish(string messageTypeIdenfifier, byte[] @event, IOutboxSession outbox, MessageMetaData metaData = null)
        {
            metaData = metaData ?? new MessageMetaData();
            var endpoint = MessagingMap2.FindEndpoint(messageTypeIdenfifier);
            return Dispatch(new WrappedCommand(messageTypeIdenfifier, @event), metaData, endpoint, outbox);
        }
        public Task PublishSynchronously(IEvent @event, MessageMetaData metaData = null)
        {
            var endpoint = MessagingMap2.FindEndpoint(@event);
            return Dispatch(@event, metaData);
        }

        internal Task Reply(IReply reply, IEndpoint2 endpoint, IOutboxSession outbox, MessageMetaData metaData = null, MessageMetaData commandMetaData = null)
        {
            metaData = metaData ?? new MessageMetaData();
            metaData.PopulateForReplyTo(commandMetaData);
            return Dispatch(reply, metaData, endpoint, outbox);
        }

        public Task Dispatch(IMessage message, MessageMetaData metaData = null, IOutboxSession outbox = null)
        {
            var endpoint = MessagingMap2.FindEndpoint(message) ??
                throw new ApplicationException($"Unable to dispatch message. Type: {message.GetType()} Message Type: {message.MessageType}");

            return Dispatch(message, metaData, endpoint, outbox);
        }

        public Task Dispatch(IMessage message,  MessageMetaData metaData, IEndpoint2 endpoint, IOutboxSession outboxSession = null)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            
            var resultContext = endpoint.TempOutboundProcess(new MessageFilterContext(message, metaData, endpoint, null));

            if (outboxSession != null)//dispatch through the outbox
            {
                OutboxDispatchOptions options = new OutboxDispatchOptions(){};
                if (metaData != null)
                {
                    if (metaData.DispatchDelay.HasValue && !endpoint.SupportsDelayedDispatch)
                    {
                        options.Delay = metaData.DispatchDelay;
                        options.SkipTransientDispatch = true; // for safety because we set delay
                    }
                    else
                        options.SkipTransientDispatch = metaData.SkipTransientDispatch;

                    options.ExpiresAtUtc = metaData.ExpiresAtUtc;
                }
                return outboxSession.Dispatch(message.MessageType, resultContext.TransportMessage.Data, resultContext.TransportMessage.MetaData, endpoint, options);
            }
            //dispatch to the endpoint
            return DispatchCore(message.MessageType, resultContext.TransportMessage.Data, resultContext.TransportMessage.MetaData, endpoint);
        }

        /// <summary>
        /// Send a serialized message to an endpoint bypassing the outbound pipeline
        /// useful for services relay messages like the outbox 
        /// </summary>
        /// <param name="messageTypeIdentifier"></param>
        /// <param name="message"></param>
        /// <param name="metaData"></param>
        /// <param name="endpointName"></param>
        /// <returns></returns>
        public Task DispatchCore(string messageTypeIdentifier, byte[] message, MessageMetaData metaData, string endpointName, bool isTransient = false)
        {
            var endpoint = endpointName != null ? MessagingMap2.FindEndpointByName(endpointName) : MessagingMap2.FindEndpoint(messageTypeIdentifier);
            if (endpoint == null)
                throw new ApplicationException($"Unable to dispatch message. Endpoint not found. MessageTypeIdentifier: {messageTypeIdentifier}. Endpoint: {endpointName}");

            if(isTransient && metaData != null)
            {
                if (metaData.SkipTransientDispatch)
                    throw new ApplicationException($"Unable to dispatch transient message.  SkipTransient was set to True. MessageTypeIdentifier: {messageTypeIdentifier}. Endpoint: {endpointName}");

                if (metaData.DispatchDelay.HasValue && !endpoint.SupportsDelayedDispatch)
                    throw new ApplicationException($"Unable to dispatch transient message.  Delay not supported by transport. MessageTypeIdentifier: {messageTypeIdentifier}. Endpoint: {endpointName}");
            }
            return DispatchCore(messageTypeIdentifier, message, metaData, endpoint);
        }

        /// <summary>
        /// Send a serialized message to an endpoint bypassing the outbound pipeline
        /// useful for services relay messages like the outbox 
        ///</summary>
        public Task DispatchCore(string messageTypeIdentifier, byte[] message, MessageMetaData metaData, IEndpoint2 endpoint)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));
          
            return endpoint.Dispatch(messageTypeIdentifier, message, metaData);
        }

    
        private void _setReplyTo(IEndpoint2 commandEndpoint, ICommand command, MessageMetaData commandMeta)
        {
            /*if(commandMeta == null)
                throw new InvalidOperationException("Command cannot be sent.  Unable to set replyTo on null MessageMetaData");

            if (!String.IsNullOrEmpty(commandMeta.ReplyTo))
                return;

            if (!String.IsNullOrEmpty(commandMeta.ReplyToEndpointName))
            {
                var replyToEndpoint = MessagingMap2.FindEndpointByName(commandMeta.ReplyToEndpointName);
                replyToEndpoint.SetReplyToForCommand(command, commandMeta);
                return;
            }
            else if(MessagingMap2.DefaultReplyToEndpoint != null)
            {
                MessagingMap2.DefaultReplyToEndpoint.SetReplyToForCommand(command, commandMeta);
                return;
            }
            */

            throw new InvalidOperationException("Command cannot be sent because replyTo endpoint could not be determined");

        }


        /****************** Initialization ********************/

        /// <summary>
        /// Begin outbox and endpoint handling/dispatching
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
    
            //foreach (var endpoint in MessagingMap2.FindAllEndpoints())
            //    await endpoint.StartAsync(this.Handle, cancellationToken);

            await _outboxManager.StartAsync(cancellationToken);

        }

        public async Task StopAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            //foreach (var endpoint in MessagingMap2.FindAllEndpoints())
            //    await endpoint.StopAsync(cancellationToken);

            await _outboxManager.StopAsync();
        }

        /// <summary>
        /// Add a new endpoint = messaging source / destination
        /// </summary>
        /// <param name="endpointSettings"></param>
        // public void AddEndpoint(EndpointSettings endpointSettings)
        // {
        //     IEndpoint2 endpoint = new Endpoint2(endpointSettings);
        //     AddEndpoint(endpoint);
        // }

        public void AddEndpoint(IEndpoint2 endpoint)
        {
          
            //MessagingMap.AddEndpoint(endpoint.Name, endpoint);
            
            //we only need to bind routes to endpoints that we write messages to because we need an address/connectionString to send them to 
            //as opposed to inbound messages which just show up at the endpoint and can validated / filtered there
            /*if (endpoint.IsWriter)
            {
                foreach (Type messageType in endpoint.BoundMessageTypes)
                {
                    var props = MessagingMap.AddMessageTypeIfNotExists(messageType);
                    MessagingMap.AddEndpointRoute(props.MessageTypeIdentifier, endpoint);
                }

                foreach (string identifier in endpoint.BoundMessageIdentifiers)
                    MessagingMap.AddEndpointRoute(identifier, endpoint);
                
                if (endpoint.Settings.IsEventDefault)
                    MessagingMap.SetDefaultPublishToEndpoint(endpoint);
 
                if (endpoint.Settings.IsReplyDefault)
                    MessagingMap.SetDefaultReplyToEndpoint(endpoint);

                //convention can be overriden by any endpoint marked as default
                if (endpoint.Name.Equals("Events", StringComparison.OrdinalIgnoreCase) && MessagingMap.DefaultPublishToEndpoint == null)
                    MessagingMap.SetDefaultPublishToEndpoint(endpoint);

                if (endpoint.Name.Equals("Replies", StringComparison.OrdinalIgnoreCase) && MessagingMap.DefaultReplyToEndpoint == null)
                    MessagingMap.SetDefaultReplyToEndpoint(endpoint);
            }*/
            
        } 

        /// <summary>
        /// Configure the Messaging Service with the provided profile
        /// </summary>
        /// <param name="profile"></param>
        // private void _configure(Profile profile)
        // {
        //     foreach (var endpointSettings in profile.EndpointSettings)
        //     {
        //         AddEndpoint(endpointSettings);
        //     }
        // }

        /// <summary>
        /// Register all framework components (message types, handlers, extractors, etc..)
        /// </summary>
        /// <param name="assemblies"></param>
        public static void InitializeTypes(params Assembly[] assemblies)
        {
            //MessagingMap2.InitializeTypes(assemblies);
        }


    }

}
