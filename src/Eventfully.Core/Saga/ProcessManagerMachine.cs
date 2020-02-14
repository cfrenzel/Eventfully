using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Eventfully
{
    /// <summary>
    /// An alias for custom message handling used specifically for ProcessManagerMachines
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMachineMessageHandler<T> : ICustomMessageHandler<T> { }

    public interface IProcessManagerMachineState
    {
        string CurrentState { get; set; }
    }

    /// <summary>
    /// When the state doesn't have a handler for the event.  
    /// Could be because events arrived out of order 
    /// the exception should trigger retry later
    /// </summary>
    public class InvalidMessageForStateException : Exception
    {
        public InvalidMessageForStateException(string state, Type messageType) 
            : base($"MessageType: {messageType} can not be handled while in State: {state}.  Message may be out of order, duplicate, error")
        { }
    }

   
    public abstract class ProcessManagerMachine<S, K> :  Saga<S, K>
        where S: IProcessManagerMachineState
    {
        /// <summary>
        /// This is not shared by all ProcessManagerMachines.  There is one cache per concrete type
        /// </summary>
        private static Dictionary<string, MethodInfo> _stateMethodCache = new Dictionary<string, MethodInfo>();
        private HandlerState _currentHandlers { get; set; }
        public ProcessManagerMachine(){}

        

        public override void SetState(S state)
        {
            base.SetState(state);
            if (!String.IsNullOrEmpty(this.State.CurrentState))
                Become(_getActionForState(MapStateToMethodName(this.State.CurrentState)));
        }

        public void Become(Action newState)
        {
            var stateName = MapMethodNameToState(newState.Method.Name);
            if (String.IsNullOrEmpty(stateName))
                throw new InvalidOperationException("Handler tasks must be named method calls");

            
            this._currentHandlers = new HandlerState(stateName);
            this.State.CurrentState = stateName;
            newState.Invoke();
        }

        /// <summary>
        /// Register a handler within a State
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        protected void Handle<T>(Func<T, MessageContext, Task> handler) where T : IIntegrationMessage
        {
            this._currentHandlers.Add<T>(handler);
        }

        /// <summary>
        /// Maps from a state to a method name
        /// This method can be overriden to handle versioning of old states etc..
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        protected virtual string MapStateToMethodName(string state)
        {
            return state;
        }

        /// <summary>
        /// Maps from a methodName to a state
        /// This method can be overriden to handle versioning of old states etc..
        /// </summary>
        /// <param name="methodName"></param>
        /// <returns></returns>
        protected virtual string MapMethodNameToState(string methodName)
        {
            return methodName;
        }

        public Task Handle(IIntegrationMessage message, MessageContext context)
        {
            var messageType = message.GetType();
            if (_currentHandlers == null)
                throw new InvalidMessageForStateException(null, messageType);

            var handler = _currentHandlers.GetHandler(messageType);
            if (handler == null)
                return Unhandled(message, context);
            else
                return handler.Invoke(message, context);
        }

        protected virtual Task Unhandled(IIntegrationMessage message, MessageContext context)
        {
            throw new InvalidMessageForStateException(_currentHandlers.StateName, message.GetType());
        }

        private Action _getActionForState(string state)
        {
            MethodInfo stateMethod = null;
            if (_stateMethodCache.ContainsKey(state))
                stateMethod = _stateMethodCache[state];
            else
            {
                stateMethod = this.GetType().GetMethod(state);
                _stateMethodCache.Add(state, stateMethod);
            }
            return (Action)Delegate.CreateDelegate(typeof(Action), this, stateMethod);
        }
    }


    public class HandlerState
    {
        public readonly string StateName;

        private readonly Dictionary<Type, Func<IIntegrationMessage, MessageContext, Task>> _handlers = new Dictionary<Type, Func<IIntegrationMessage, MessageContext, Task>>();

        public HandlerState(string stateName)
        {
            this.StateName = stateName;
        }

        public void Add<T>(Func<T, MessageContext, Task> handler)
            where T : IIntegrationMessage
        {
            if (handler == null)
                throw new ArgumentNullException("Handler can not be null");
            var type = typeof(T);
            if (_handlers.ContainsKey(type))
                throw new InvalidOperationException($"Duplicate Handler detected.  Cannot add handler for {type}");

            //wrap Action to convert from IIntegrationMessage back to the Type (T)
            _handlers.Add(type,
                new Func<IIntegrationMessage, MessageContext, Task>((untyped, context) => {
                    var typed = (T)Convert.ChangeType(untyped, typeof(T));
                    return handler(typed, context);
                }));
        }

        public Func<IIntegrationMessage, MessageContext, Task> GetHandler(Type messageType)
        {
            if (_handlers.ContainsKey(messageType))
                return _handlers[messageType];
            return null;
        }
    }


}
