using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Eventfully
{
    
    public interface IMachineMessageHandler<T>
    {
        //Task Handle(T message, MessageContext context);
    }

    public interface IProcessManagerMachineState
    {
        string CurrentState { get; set; }
    }

    public class InvalidProcessManagerStateException : Exception
    {
        public InvalidProcessManagerStateException(string state, Type messageType) : base($"MessageType: {messageType} can not be handled while in State: {state}")
        { }
    }

    public class HandlerState
    {
        public readonly string StateName;

        private readonly Dictionary<Type, Action<IIntegrationMessage, MessageContext>> _handlers = new Dictionary<Type, Action<IIntegrationMessage, MessageContext>>();

        public HandlerState(string stateName)
        {
            this.StateName = stateName;
        }

        public void Add<T>(Action<T, MessageContext> handler)
            where T : IIntegrationMessage
        {
            if (handler == null)
                throw new ArgumentNullException("Handler can not be null");
            var type = typeof(T);
            if (_handlers.ContainsKey(type))
                throw new InvalidOperationException($"Duplicate Handler detected.  Cannot add handler for {type}");
            
            //convert from IIntegrationMessage back to the Type (T)
            _handlers.Add(type, 
                new Action<IIntegrationMessage, MessageContext>((untyped, context) => {
                    var typed = (T)Convert.ChangeType(untyped, typeof(T)); 
                    handler(typed, context); 
              }));
        }
        public Action<IIntegrationMessage, MessageContext> GetHandler(Type messageType)
        {
            if (_handlers.ContainsKey(messageType))
                return _handlers[messageType];
            return null;
        }
    }

    public interface IProcessManagerMachine
    {
        void HandleCore(IIntegrationMessage message, MessageContext context);
        //void Become(Action newState);
        void SetState(Object state);
    }


    public abstract class ProcessManagerMachine<S, K> :  Saga<S, K>, IProcessManagerMachine
        where S: IProcessManagerMachineState
    {
        //private Stack<HandlerState> _handlerStack { get; set; } = new Stack<HandlerState>();
        private HandlerState _currentHandlers; 

        //public abstract Action InitialState { get; }

        public ProcessManagerMachine()
        {
            SetState((S)Activator.CreateInstance(typeof(S)));
        }

        void IProcessManagerMachine.SetState(object state) => SetState((S)state);
       
        public void SetState(S state)
        {
            if (state == null)
                throw new ArgumentNullException("SetState requires a State object.  State cannot be null");
            this.State = state;
            
            //if(String.IsNullOrEmpty(this.State.CurrentState))
            //    throw new ArgumentNullException("ProcessManagerMachine requires a valid State.CurrentState");

            if (!String.IsNullOrEmpty(this.State.CurrentState))
            {
                MethodInfo stateMethod = this.GetType().GetMethod(this.State.CurrentState);
                Become((Action)Delegate.CreateDelegate(typeof(Action), stateMethod));
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

        protected void Handle<T>(Action<T, MessageContext> handler) where T : IIntegrationMessage
        {
            this._currentHandlers.Add<T>(handler);// as Action<IIntegrationMessage, MessageContext>);
        }

        public void HandleCore(IIntegrationMessage message, MessageContext context)
        {
            var messageType = message.GetType();
            var handler = _currentHandlers.GetHandler(messageType);
            if (handler == null)
                throw new InvalidProcessManagerStateException(_currentHandlers.StateName, messageType);
            handler.Invoke(message, context);
        }

      
    }



    //public class TestSaga : ProcessManagerMachine<PizzaSagaState, Guid>,
    //    IMachineMessageHandler<PizzaOrderedEvent>,
    //    IMachineMessageHandler<PizzaPaidForEvent>,
    //    IMachineMessageHandler<PizzaPreparedEvent>,
    //    IMachineMessageHandler<PizzaDeliveredEvent>
    //  {
    //    public TestSaga(){
    //        Become(Ordered);
    //    }

    //    public void Ordered()
    //    {
    //        //Ignore<PizzaOrderedEvent>();
    //        Handle<PizzaPaidForEvent>((message, context) => {
    //            State.PaidAtUtc = DateTime.UtcNow;
    //            if (State.CanBeShipped)
    //                Become(Ready);
    //        });

    //        Handle<PizzaPreparedEvent>((message, context) => {
    //            State.PreparedAtUtc = DateTime.UtcNow;
    //            if (State.CanBeShipped)
    //                Become(Ready);
    //        });
    //    }

    //    public void Ready()
    //    {
    //        Handle<PizzaShippedEvent>((message, context) => {
    //            State.ShippedAtUtc = DateTime.UtcNow;
    //            Become(OutForDelivery);
    //        });
    //    }

    //    public void OutForDelivery()
    //    {
    //        Handle<PizzaDeliveredEvent>((message, context) => {
    //            State.DeliveredAtUtc = DateTime.UtcNow;
    //            Become(Delivered);
    //        });
    //    }

    //    public void Delivered()
    //    {
    //        Handle<PizzaPaidForEvent>((message, context) => {

    //        });

    //        Handle<PizzaPreparedEvent>((message, context) => {

    //        });
    //    }
    //}


    //public class PizzaSagaState : IProcessManagerMachineState
    //{
    //    public DateTime? PreparedAtUtc { get; set; }
    //    public DateTime? PaidAtUtc { get; set; }
    //    public DateTime? ShippedAtUtc { get; set; }
    //    public DateTime? DeliveredAtUtc { get; set; }
    //    public bool CanBeShipped { get { return PreparedAtUtc.HasValue && PaidAtUtc.HasValue; } }
    //    public string CurrentState { get; set; }
    //    public byte[] RowVersion { get; set; }
    //}

    //public class PizzaOrderedEvent : IntegrationEvent
    //{
    //    public override string MessageType => "Pizza.Ordered";
    //}
    //public class PizzaPaidForEvent : IntegrationEvent
    //{
    //    public override string MessageType => "Pizza.PaidFor";
    //}
    //public class PizzaPreparedEvent : IntegrationEvent
    //{
    //    public override string MessageType => "Pizza.Prepared";
    //}
    //public class PizzaDeliveredEvent : IntegrationEvent
    //{
    //    public override string MessageType => "Pizza.Delivered";
    //}
    //public class PizzaShippedEvent : IntegrationEvent
    //{
    //    public override string MessageType => "Pizza.Shipped";
    //}


}
