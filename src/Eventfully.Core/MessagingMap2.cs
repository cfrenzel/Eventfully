using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Eventfully
{

    /// <summary>
    /// The general mapping and caching of message types / endpoints / handlers / sagas
    /// needed to serialize/deserialize dispatch and handle messages
    /// </summary>
    public static class MessagingMap2
    {
        private static ConcurrentDictionary<string, MessageTypeProperties2> _messageTypeIdentifierPropMap = new ConcurrentDictionary<string, MessageTypeProperties2>();
        private static ConcurrentDictionary<Type, MessageTypeProperties2> _messageTypePropMap = new ConcurrentDictionary<Type, MessageTypeProperties2>();
        private static ConcurrentDictionary<Type, SagaProperties2> _sagaTypePropMap = new ConcurrentDictionary<Type, SagaProperties2>();

        private static readonly ConcurrentDictionary<string, IEndpoint2> _nameEndpointMap = new ConcurrentDictionary<string, IEndpoint2>();
        private static readonly ConcurrentDictionary<string, IEndpoint2> _messageTypeIdentifierRouteToEndpointMap = new ConcurrentDictionary<string, IEndpoint2>();

        private static Type _extractorInterface = typeof(IMessageExtractor);

        // public static bool HasHandler(IEndpoint2 endpoint)
        // {
        //     foreach (var messageType in endpoint.MessageTypes)
        //     {
        //         var props = GetProps(messageType);
        //         if (props.HasHandler)
        //             return true;
        //     }
        //     return false;
        // }
        
        public static MessageTypeProperties2 AddMessageType(Type type)
        {
            if (_messageTypePropMap.ContainsKey(type))
                throw new InvalidOperationException($"Duplicate Message Type registered.  MessageType: {type}");

            var message = (IMessage)Activator.CreateInstance(type, true);
            var props = new MessageTypeProperties2(type, message.MessageType, _extractorInterface.IsAssignableFrom(type));
            AddMessageType(props);
            return props;
        }

        public static MessageTypeProperties2 AddMessageTypeIfNotExists(Type type)
        {
            if (_messageTypePropMap.ContainsKey(type))
                return GetProps(type);

            return AddMessageType(type);
        }


        public static void AddMessageType(MessageTypeProperties2 props)
        {
            var handlingSaga = _sagaTypePropMap.SingleOrDefault(x => x.Value.MessageTypes.Contains(props.Type));
            if (handlingSaga.Key != null)
                props.SagaType = handlingSaga.Key;

            _messageTypeIdentifierPropMap.TryAdd(props.MessageTypeIdentifier, props);
            _messageTypePropMap.TryAdd(props.Type, props);
        }

        public static string GetMessageTypeIdentifier(Type messageType)
        {
            _messageTypePropMap.TryGetValue(messageType, out MessageTypeProperties2 props);
            return props?.MessageTypeIdentifier;
        }
        public static Type GetMessageType(string typeIdentifier)
        {
            _messageTypeIdentifierPropMap.TryGetValue(typeIdentifier, out MessageTypeProperties2 props);
            return props?.Type;
        }

        public static MessageTypeProperties2 GetProps(string typeIdentifier)
        {
            _messageTypeIdentifierPropMap.TryGetValue(typeIdentifier, out MessageTypeProperties2 props);
            return props;
        }
        public static MessageTypeProperties2 GetProps(Type type)
        {
            _messageTypePropMap.TryGetValue(type, out MessageTypeProperties2 props);
            return props;
        }

        public static IEnumerable<IEndpoint2> FindAllEndpoints()
        {
            return _nameEndpointMap.Values;
        }
        
        public static IEndpoint2 FindEndpointByName(string name)
        {
            if (_nameEndpointMap.TryGetValue(name, out IEndpoint2 endpoint))
                return endpoint; 
            throw new EndpointNotFoundException(name, null);
        }

        public static IEndpoint2 FindEndpoint(string messageTypeIdenfifier)
        {
            if (_messageTypeIdentifierRouteToEndpointMap.TryGetValue(messageTypeIdenfifier, out IEndpoint2 endpoint))
                return endpoint;
            throw new EndpointNotFoundException(messageTypeIdenfifier);
        }
        
        public static IEndpoint2 FindEndpoint(IMessage message)
        {
            if (_messageTypeIdentifierRouteToEndpointMap.TryGetValue(message.MessageType, out IEndpoint2 endpoint))
                return endpoint;
            // else if (message is IEvent)
            // {
            //     //check for a default route for events and adding to the route map
            //     if (_defaultPublishToEndpoint != null)
            //     {
            //         _messageTypeIdentifierRouteToEndpointMap.Add(message.MessageType, _defaultPublishToEndpoint);
            //         return _defaultPublishToEndpoint;
            //     }
            // }
            throw new EndpointNotFoundException(message.MessageType);
        }

        public static void AddEndpoint(string name, IEndpoint2 endpoint)
        {
            _nameEndpointMap.TryAdd(endpoint.Name, endpoint);
        }

        // public static void AddEndpointRoute(string messageTypeIdentifier, IEndpoint2 endpoint)
        // {
        //     _messageTypeIdentifierRouteToEndpointMap.Add(messageTypeIdentifier, endpoint);
        // }
        
        public static SagaProperties2 AddSaga(Type sagaType, Type sagaKeyType, Type sagaStateType, Type sagaPersistenceType, List<Type> handledMessageTypes, bool hasCustomHandler = false)
        {
            if (_sagaTypePropMap.ContainsKey(sagaType))
                throw new InvalidOperationException($"Duplicate Saga Type registered.  SagaType: {sagaType}");

            var props = new SagaProperties2(sagaType, sagaKeyType, sagaStateType, sagaPersistenceType, handledMessageTypes, hasCustomHandler);
            AddSaga(props);
            return props;
        }

        public static void AddSaga(SagaProperties2 props)
        {
            _sagaTypePropMap.TryAdd(props.SagaType, props);

            //link to the saga from the messageType
            foreach (var messageType in props.MessageTypes)
            {
                if(_messageTypePropMap.ContainsKey(messageType))
                    _messageTypePropMap[messageType].SagaType = props.SagaType;
            }
        }

        public static SagaProperties2 GetSagaProps(Type t)
        {
            if (_sagaTypePropMap.TryGetValue(t, out SagaProperties2 props))
                return props;
            return null;
        }
        
        
    }

    public class MessageTypeProperties2
    {
        public Type Type { get; set; }

        public string MessageTypeIdentifier { get; set; }

        public bool HasCustomExtractor { get; set; }

        public bool HasSagaHandler { get { return SagaType != null; }  }

        public Type SagaType { get; set; }

        
        public MessageTypeProperties2()
        { }

        public MessageTypeProperties2(Type type, string messageTypeIdentifier, bool hasCustomExtractor = false, Type sagaType = null)
        {
            this.Type = type;
            this.MessageTypeIdentifier = messageTypeIdentifier;
            this.HasCustomExtractor = hasCustomExtractor;
            this.SagaType = sagaType;
        }
    }

    public class SagaProperties2
    {
        public Type SagaType { get; set; }
        public Type StateType { get; set; }
        public Type KeyType { get; set; }
        public Type SagaPersistenceType { get; set; }
        public bool HasCustomHandler { get; set; }
        public List<Type> MessageTypes { get; set; } = new List<Type>();


        public SagaProperties2()
        { }

        public SagaProperties2(Type sagaType, Type keyType, Type stateType, Type sagaPersistenceType , List<Type> messageTypes, bool hasCustomhandler = false)
        {
            this.SagaType = sagaType;
            this.KeyType = keyType;
            this.StateType = stateType;
            this.SagaPersistenceType = sagaPersistenceType;
            this.HasCustomHandler = hasCustomhandler;

            if (messageTypes != null)
                MessageTypes = messageTypes;
        }
    }
}
