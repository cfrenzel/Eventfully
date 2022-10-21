using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eventfully.Outboxing;

namespace Eventfully.Handlers
{
    /// <summary>
    /// Inspired by https://github.com/jbogard/MediatR/blob/master/LICENSE
    /// </summary>
    public class MessageDispatcher : IMessageDispatcher
    {
        private readonly IServiceFactory _handlerFactory;

        public MessageDispatcher(IServiceFactory handlerFactory)
        {
            if (handlerFactory == null) throw new ArgumentNullException(nameof(handlerFactory));
                _handlerFactory = handlerFactory;
        }

        public async Task Dispatch(IMessage message, MessageContext context)
        {
            var handler = GetHandler(message);
            await handler.Handle(message, context, _handlerFactory);
        }

        private static MessageDispatcherHandler GetHandler(IMessage message)
        {
            var genericDispatcherType = typeof(MessageDispatcherHandler<>)
                .MakeGenericType(message.GetType());

            return (MessageDispatcherHandler)
                Activator.CreateInstance(genericDispatcherType);
        }

        private abstract class MessageDispatcherHandler
        {
            public abstract Task Handle(IMessage message, MessageContext context, IServiceFactory handlerFactory);
        }


        private class MessageDispatcherHandler<T> : MessageDispatcherHandler
            where T : IMessage
        {
            public override Task Handle(IMessage message, MessageContext context, IServiceFactory handlerFactory)
            {
                return HandleCore((T)message, context, handlerFactory);
            }

            private static async Task HandleCore(T message, MessageContext context, IServiceFactory handlerFactory)
            {
                using (var scope = handlerFactory.CreateScope())//for dependency injection
                {
                    context.OutboxSession = scope.GetInstance<IOutboxSession>();
              
                    //is there a processManager/Saga for this message type
                    if (context.Props != null && context.Props.HasSagaHandler)
                    {
                        var sagaProps = MessagingMap2.GetSagaProps(context.Props.SagaType) ??
                            throw new ApplicationException($"Couldn't find saga for Handler: {context.Props.Type}, SagaType: {context.Props.SagaType}");
                        
                        var saga = (ISaga)scope.GetInstance(sagaProps.SagaType);
                        
                        var persistence = scope.GetInstance(sagaProps.SagaPersistenceType) as ISagaPersistence;
                        await persistence.LoadOrCreateState(saga, saga.FindKey(message, context.MetaData));

                        if (saga is IMessageHandler<T>)//includes ITriggeredBy<T>
                            await ((IMessageHandler<T>)saga).Handle(message, context);
                        else if(saga is ICustomMessageHandler<T>)
                            await ((ICustomMessageHandler<T>)saga).Handle(message, context); 
                        
                        await persistence.AddOrUpdateState(saga);
                    }
                    else // basic handler class
                    {
                        var handler = scope.GetInstance<IMessageHandler<T>>();
                        await handler.Handle(message, context);
                    }
                }
            }


        }



    }
}
   
