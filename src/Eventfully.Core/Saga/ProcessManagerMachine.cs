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
    public class InvalidProcessManagerStateException : Exception
    {
        public InvalidProcessManagerStateException(string state, Type messageType) 
            : base($"MessageType: {messageType} can not be handled while in State: {state}")
        { }
    }

   
    public abstract class ProcessManagerMachine<S, K> :  Saga<S, K>
        where S: IProcessManagerMachineState
    {
        private HandlerState _currentHandlers { get; set; }
        public ProcessManagerMachine(){}

        public override void SetState(S state)
        {
            base.SetState(state);
            if (!String.IsNullOrEmpty(this.State.CurrentState))
            {
                MethodInfo stateMethod = this.GetType().GetMethod(this.State.CurrentState);
                Become((Action)Delegate.CreateDelegate(typeof(Action), this, stateMethod));
            }
        }

        public void Become(Action newState)
        {
            var stateName = newState.Method.Name;
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

        public Task Handle(IIntegrationMessage message, MessageContext context)
        {
            var messageType = message.GetType();
            if (_currentHandlers == null)
                throw new InvalidProcessManagerStateException(null, messageType);

            var handler = _currentHandlers.GetHandler(messageType);
            if (handler == null)
                throw new InvalidProcessManagerStateException(_currentHandlers.StateName, messageType);
            return handler.Invoke(message, context);
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

            //warp Action to convert from IIntegrationMessage back to the Type (T)
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
