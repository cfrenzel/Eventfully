using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace Eventfully.Transports.AzureServiceBus
{
    public class AzureServiceBusTransportSettings : TransportSettings
    {
        private readonly ITransportFactory _factory = new AzureServiceBusTransportFactory();
        
        public override ITransportFactory Factory  => _factory; 

        public AzureServiceBusTransportSettings(){}

        public bool SpecialFeature { get;  set; }

        public AzureServiceBusTransportSettings UseSpecialFeature()
        {
            this.SpecialFeature = true;
            return this;
        }
    }


    public class AzureServiceBusTransport : ITransport
    {
        private readonly TransportSettings _settings;
        private readonly AzureServiceBusMetaDataMapper _metaDataMapper;
        private AzureServiceBusMessagePump _messagePump;
        private Task _messagePumpTask;
        private Handler _handler;

        /// <summary>
        /// Tuple<string,string> (Endpoint,EntityPath) ("sb://app.servicebus.windows.net",  "assets/events")
        /// </summary>
        private static readonly Dictionary<Tuple<string, string>, IEndpoint> _endpointsByEntity = new Dictionary<Tuple<string, string>, IEndpoint>();
        private static readonly Dictionary<string, IEndpoint> _replyToCache = new Dictionary<string, IEndpoint>();
        private static readonly Dictionary<string, string> _replyToRouteCache = new Dictionary<string, string>();

       
        public bool SupportsDelayedDispatch => true;

        public AzureServiceBusTransport(TransportSettings settings)
        {
            _settings = settings;
            _metaDataMapper = new AzureServiceBusMetaDataMapper();
        }

        public Task StartAsync(IEndpoint endpoint, Handler handler, CancellationToken cancellationToken = default(CancellationToken))
        {
            var connBuilder = new ServiceBusConnectionStringBuilder(endpoint.Settings.ConnectionString);
            var key = new Tuple<string, string>(connBuilder.Endpoint, connBuilder.EntityPath);
            if (!_endpointsByEntity.ContainsKey(key))
            {
                _handler = handler;
                //validate the connection string and store it for later lookup by entityPath and endpoint
                _endpointsByEntity.Add(key, endpoint);

                if (endpoint.IsReader)
                {
                    if (_messagePump == null)
                        _messagePump = new AzureServiceBusMessagePump((m, e) => Handle(m, e), endpoint, _metaDataMapper);
                    return _messagePumpTask = _messagePump.StartAsync(cancellationToken);
                }
            }
             return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            if (_messagePump != null)
                return _messagePump.StopAsync();

            return Task.CompletedTask;
        }

        public Task Handle(TransportMessage transportMessage, IEndpoint endpoint)
        {
            return _handler.Invoke(transportMessage, endpoint);
            //return MessagingService.Instance.Handle(transportMessage, endpoint);
        }

        public Task Dispatch(string messageTypeIdenfifier, byte[] message, IEndpoint endpoint, MessageMetaData metaData = null)
        {
            return _dispatch(messageTypeIdenfifier, message, endpoint, metaData);
        }

     
        protected Task _dispatch(string messageTypeIdentifier, byte[] messageBody, IEndpoint endpoint, MessageMetaData meta = null)
        {
            var message = new Message(messageBody);
            var sender = AzureServiceBusClientCache.GetSender(endpoint.Settings.ConnectionString);
            _metaDataMapper.ApplyMetaData(message, meta, messageTypeIdentifier);

            DateTime? scheduleAtUtc = null;
            if (meta != null && meta.DispatchDelay.HasValue)
            {
                //var baseDate = meta.CreatedAtUtc.HasValue ? meta.CreatedAtUtc.Value : DateTime.UtcNow;
                var baseDate = DateTime.UtcNow;
                scheduleAtUtc = baseDate.Add(meta.DispatchDelay.Value);
            }
            if(scheduleAtUtc.HasValue)// && scheduleAtUtc > DateTime.UtcNow.AddMilliseconds(400))
                return sender.ScheduleMessageAsync(message, scheduleAtUtc.Value);
            return sender.SendAsync(message);
        }

        public IEndpoint FindEndpointForReply(MessageContext commandContext)
        {
            var replyTo = commandContext.MetaData != null ? commandContext.MetaData.ReplyTo : null;
            if (String.IsNullOrEmpty(replyTo))
                throw new InvalidOperationException("Azure service bus commands must set ReplyTo");

            if (_replyToCache.ContainsKey(replyTo))
                return _replyToCache[replyTo];

            string endpoint = null;
            string entityPath = null;
            if(!_parsePartialConnectionString(replyTo, out endpoint, out entityPath))
                throw new ApplicationException("Invalid replyTo for AzureServiceBusTransport. Unable to find entity path and endpoint.  Should contain ';' seperated endpoint and entitypath");

            //IEndpoint endpointByEndpoint = null;
            IEndpoint endpointByEntity = null;          
            if (_endpointsByEntity.TryGetValue(new Tuple<string, string>(endpoint, entityPath), out endpointByEntity))
            {
                _replyToCache.Add(replyTo, endpointByEntity);
                return endpointByEntity;
            }
            ////connection string for the endpoint that will work with any entity
            //else if (_endpointsByEntity.TryGetValue(new Tuple<string, string>(endpoint, null), out endpointByEndpoint))
            //{
            //    var newConn = new ServiceBusConnectionStringBuilder(endpointByEndpoint.Settings.ConnectionString);
            //    newConn.EntityPath = entityPath;
            //    var newEndpoint =  new Endpoint(
            //        new EndpointSettings($"Computed-{entityPath}", newConn.GetEntityConnectionString())
            //        {
            //            TransportSettings = this._settings
            //        }.AsOutbound()
            //    );
            //    _replyToCache.Add(replyTo, newEndpoint);
            //    MessagingService.Instance.AddEndpoint(newEndpoint);
            //    return newEndpoint;
            //}
            return null;
        }


        public void SetReplyToForCommand(IEndpoint endpoint, IIntegrationCommand command, MessageMetaData meta)
        {
            meta = meta ?? new MessageMetaData();
            //if (meta == null)
            //    throw new InvalidOperationException("Cannot set reply to on null MessageMetaData");

            if (!_replyToRouteCache.ContainsKey(endpoint.Name))
            {
                var connBuilder = new ServiceBusConnectionStringBuilder(endpoint.Settings.ConnectionString);
                string sbEndpoint = connBuilder.Endpoint;
                string sbEntityPath = connBuilder.EntityPath;
                if (!String.IsNullOrEmpty(sbEndpoint) && !String.IsNullOrEmpty(sbEntityPath)) 
                    _replyToRouteCache[endpoint.Name] = $"Endpoint={sbEndpoint};EntityPath={sbEntityPath}";
            }
            meta.ReplyTo = _replyToRouteCache[endpoint.Name];
        }


        private bool _parsePartialConnectionString(string replyTo, out string endpoint, out string entityPath)
        {
            endpoint = null;
            entityPath = null;
            var parts = replyTo.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                throw new ApplicationException("Invalid replyTo for AzureServiceBusTransport.  Should contain ';' seperated endpoint and entitypath");

            foreach (var part in parts)
            {
                var cleanPart = part.Trim().ToLower();
                if (cleanPart.StartsWith("endpoint="))
                    endpoint = cleanPart.Substring(9);
                else if (cleanPart.StartsWith("entitypath="))
                    entityPath = cleanPart.Substring(11);
            }
            return !String.IsNullOrEmpty(endpoint) && !String.IsNullOrEmpty(entityPath);
        }

     
    }

}
