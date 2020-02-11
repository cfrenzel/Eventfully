using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Eventfully
{

    /// <summary>
    /// The general mapping and caching of names / endpoints / classes / handlers / sagas
    /// to serialize/deserialize dispatch and handle messages
    /// </summary>
    public static class MessagingMap
    {
        private static ConcurrentDictionary<string, MessageTypeProperties> _messageTypeIdentifierPropMap = new ConcurrentDictionary<string, MessageTypeProperties>();
        private static ConcurrentDictionary<Type, MessageTypeProperties> _messageTypePropMap = new ConcurrentDictionary<Type, MessageTypeProperties>();
        private static ConcurrentDictionary<Type, SagaProperties> _sagaTypePropMap = new ConcurrentDictionary<Type, SagaProperties>();

        private static readonly Dictionary<string, IEndpoint> _nameEndpointMap = new Dictionary<string, IEndpoint>();
        private static readonly Dictionary<string, IEndpoint> _messageTypeIdentifierRouteToEndpointMap = new Dictionary<string, IEndpoint>();

        private static Type _extractorInterface = typeof(IMessageExtractor);
        
        public static MessageTypeProperties AddMessageType(Type type)
        {
            if (_messageTypePropMap.ContainsKey(type))
                throw new InvalidOperationException($"Duplicate Message Type registered.  MessageType: {type}");

            var message = (IIntegrationMessage)Activator.CreateInstance(type, true);
            var props = new MessageTypeProperties(type, message.MessageType, _extractorInterface.IsAssignableFrom(type));
            AddMessageType(props);
            return props;
        }

        public static MessageTypeProperties AddMessageTypeIfNotExists(Type type)
        {
            if (_messageTypePropMap.ContainsKey(type))
                return GetProps(type);

            return AddMessageType(type);
        }


        public static void AddMessageType(MessageTypeProperties props)
        {
            var handlingSaga = _sagaTypePropMap.SingleOrDefault(x => x.Value.MessageTypes.Contains(props.Type));
            if (handlingSaga.Key != null)
                props.SagaType = handlingSaga.Key;

            _messageTypeIdentifierPropMap.TryAdd(props.MessageTypeIdentifier, props);
            _messageTypePropMap.TryAdd(props.Type, props);
        }

        public static string GetMessageTypeIdentifier(Type messageType)
        {
            MessageTypeProperties props;
            _messageTypePropMap.TryGetValue(messageType, out props);
            return props == null ? null : props.MessageTypeIdentifier;
        }
        public static Type GetMessageType(string typeIdentifier)
        {
            MessageTypeProperties props;
            _messageTypeIdentifierPropMap.TryGetValue(typeIdentifier, out props);
            return props == null ? null : props.Type;
        }

        public static MessageTypeProperties GetProps(string typeIdentifier)
        {
            MessageTypeProperties props;
            _messageTypeIdentifierPropMap.TryGetValue(typeIdentifier, out props);
            return props;
        }
        public static MessageTypeProperties GetProps(Type type)
        {
            MessageTypeProperties props;
            _messageTypePropMap.TryGetValue(type, out props);
            return props;
        }



        public static IEnumerable<IEndpoint> FindAllEndpoints()
        {
            return _nameEndpointMap.Values;
        }
        public static IEndpoint FindEndpointByName(string name)
        {
            IEndpoint endpoint = null;
            if (_nameEndpointMap.TryGetValue(name, out endpoint))
                return endpoint;

            throw new EndpointNotFoundException(name, null);
        }

        public static IEndpoint FindEndpoint(string messageTypeIdenfifier)
        {
            IEndpoint endpoint = null;
            if (_messageTypeIdentifierRouteToEndpointMap.TryGetValue(messageTypeIdenfifier, out endpoint))
                return endpoint;

            throw new EndpointNotFoundException(messageTypeIdenfifier);
        }


        public static IEndpoint FindEndpoint(IIntegrationMessage message)
        {
            IEndpoint endpoint = null;
            if (_messageTypeIdentifierRouteToEndpointMap.TryGetValue(message.MessageType, out endpoint))
                return endpoint;
            else if (message is IIntegrationEvent)
            {
                //check for a default route for events and adding to the route map
                if (_defaultPublishToEndpoint != null)
                {
                    _messageTypeIdentifierRouteToEndpointMap.Add(message.MessageType, _defaultPublishToEndpoint);
                    return _defaultPublishToEndpoint;
                }
            }
            throw new EndpointNotFoundException(message.MessageType);
        }

        public static void AddEndpoint(string name, IEndpoint endpoint)
        {
            _nameEndpointMap.Add(endpoint.Name, endpoint);
        }

        public static void AddEndpointRoute(string messageTypeIdentifier, IEndpoint endpoint)
        {
            _messageTypeIdentifierRouteToEndpointMap.Add(messageTypeIdentifier, endpoint);
        }


        private static IEndpoint _defaultPublishToEndpoint = null;
        public static IEndpoint DefaultPublishToEndpoint { get => _defaultPublishToEndpoint; }

        public static void SetDefaultPublishToEndpoint(IEndpoint endpoint)
        {
            _defaultPublishToEndpoint = endpoint;
        }

        private static IEndpoint _defaultReplyToEndpoint = null;
        public static IEndpoint DefaultReplyToEndpoint { get => _defaultReplyToEndpoint; }
        public static void SetDefaultReplyToEndpoint(IEndpoint endpoint)
        {
            _defaultReplyToEndpoint = endpoint;
        }
        public static IEndpoint GetDefaultReplyToEndpoint()
        {
            return _defaultReplyToEndpoint;
        }


        public static SagaProperties AddSaga(Type sagaType, Type sagaKeyType, Type sagaStateType, Type sagaPersistenceType, List<Type> handledMessageTypes, bool hasCustomHandler = false)
        {
            if (_sagaTypePropMap.ContainsKey(sagaType))
                throw new InvalidOperationException($"Duplicate Saga Type registered.  SagaType: {sagaType}");

            var props = new SagaProperties(sagaType, sagaKeyType, sagaStateType, sagaPersistenceType, handledMessageTypes);
            AddSaga(props);
            return props;
        }

        public static void AddSaga(SagaProperties props)
        {
            _sagaTypePropMap.TryAdd(props.Type, props);

            //link to the saga from the messageType
            foreach (var messageType in props.MessageTypes)
            {
                if(_messageTypePropMap.ContainsKey(messageType))
                    _messageTypePropMap[messageType].SagaType = props.Type;
            }
        }

        public static SagaProperties GetSagaProps(Type t)
        {
            SagaProperties props;
            if (_sagaTypePropMap.TryGetValue(t, out props))
                return props;
            return null;
        }


        public static void InitializeTypes(params Assembly[] assemblies)
        {
            var messageInterface = typeof(IIntegrationMessage);
            var handlerInterface = typeof(IMessageHandler<>);
            var customHandlerInterface = typeof(ICustomMessageHandler<>);
            var processManagerInterface = typeof(ISaga<,>);
            
            var types = assemblies.SelectMany(s => s.GetTypes());
            foreach (var type in types)
            {
                IEnumerable<Type> handlers;
                IEnumerable<Type> sagas;
             
                if (type.IsClass && !type.IsAbstract)
                {
                    //register message types
                    if (messageInterface.IsAssignableFrom(type))
                    {
                        MessagingMap.AddMessageType(type);
                    }
                    else if (IsAssignableFromGenericType(new Type[] { handlerInterface, customHandlerInterface }, type, out handlers))
                    {
                        //has a single handler for all message types
                        bool hasCustomHandler = handlers.Any(x => x.GetGenericTypeDefinition() == customHandlerInterface);

                        //register process manager types
                        if (IsAssignableFromGenericType(processManagerInterface, type, out sagas))
                        {
                            List<Type> handledMessageTypes = new List<Type>();
                            var sagaGenerics = sagas.FirstOrDefault().GenericTypeArguments;
                            var sagaStateType = sagaGenerics[0];
                            var sagaIdType = sagaGenerics[1];
                            var sagaPersistenceType = typeof(ISagaPersistence<,>).MakeGenericType(sagaStateType, sagaIdType);
                            
                            foreach (var handler in handlers)
                                handledMessageTypes.Add(handler.GenericTypeArguments.First());
                                                      
                            MessagingMap.AddSaga(type, sagaIdType, sagaStateType, sagaPersistenceType, handledMessageTypes, hasCustomHandler);
                        }
                    }
                }
            }//foreach
        }

        private static bool IsAssignableFromGenericType(Type genericType, Type givenType, out IEnumerable<Type> interfaces)
        {
            return IsAssignableFromGenericType(new Type[] { genericType }, givenType, out interfaces);
        }
        private static bool IsAssignableFromGenericType(Type[] genericTypes, Type givenType, out IEnumerable<Type> interfaces)
        {
            interfaces = givenType
             .GetInterfaces()
             .Where(it => it.IsGenericType && genericTypes.Contains(it.GetGenericTypeDefinition()));

            return interfaces != null && interfaces.Count() > 0;
        }
    }

    public class MessageTypeProperties
    {
        public Type Type { get; set; }

        public string MessageTypeIdentifier { get; set; }

        public bool HasCustomExtractor { get; set; }

        public bool HasSagaHandler { get { return SagaType != null; }  }

        public Type SagaType { get; set; }

        
        public MessageTypeProperties()
        { }

        public MessageTypeProperties(Type type, string messageTypeIdentifier, bool hasCustomExtractor = false, Type sagaType = null)
        {
            this.Type = type;
            this.MessageTypeIdentifier = messageTypeIdentifier;
            this.HasCustomExtractor = hasCustomExtractor;
            this.SagaType = sagaType;
        }
    }

    public class SagaProperties
    {
        public Type Type { get; set; }

        public Type StateType { get; set; }
        public Type KeyType { get; set; }
        public Type SagaPersistenceType { get; set; }
        public bool HasCustomHandler { get; set; }
        public List<Type> MessageTypes { get; set; } = new List<Type>();


        public SagaProperties()
        { }

        public SagaProperties(Type type, Type keyType, Type stateType, Type sagaPersistenceType , List<Type> messageTypes, bool hasCustomhandler = false)
        {
            this.Type = type;
            this.KeyType = keyType;
            this.StateType = stateType;
            this.SagaPersistenceType = sagaPersistenceType;
            this.HasCustomHandler = hasCustomhandler;

            if (messageTypes != null)
                MessageTypes = messageTypes;
        }
    }
}
