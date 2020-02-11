using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eventfully.Outboxing;

namespace Eventfully.Handlers
{
    /// <summary>
    /// Inspired by Jimmy Bogard's blog and MediatR project
    /// https://github.com/jbogard/MediatR/blob/master/LICENSE
    /// </summary>
    public class MessageDispatcher : IMessageDispatcher
    {
        private readonly IServiceFactory _handlerFactory;

        public MessageDispatcher(IServiceFactory handlerFactory)
        {
            if (handlerFactory == null) throw new ArgumentNullException(nameof(handlerFactory));
                _handlerFactory = handlerFactory;
        }

        public async Task Dispatch(IIntegrationMessage message, MessageContext context)
        {
            var handler = GetHandler(message);
            await handler.Handle(message, context, _handlerFactory);
        }

        private static IntegrationMessageDispatcherHandler GetHandler(IIntegrationMessage message)
        {
            var genericDispatcherType = typeof(IntegrationMessageDispatcherHandler<>)
                .MakeGenericType(message.GetType());

            return (IntegrationMessageDispatcherHandler)
                Activator.CreateInstance(genericDispatcherType);
        }

        private abstract class IntegrationMessageDispatcherHandler
        {
            public abstract Task Handle(IIntegrationMessage message, MessageContext context, IServiceFactory handlerFactory);
        }


        private class IntegrationMessageDispatcherHandler<T> : IntegrationMessageDispatcherHandler
            where T : IIntegrationMessage
        {
            public override Task Handle(IIntegrationMessage message, MessageContext context, IServiceFactory handlerFactory)
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
                        var sagaProps = MessagingMap.GetSagaProps(context.Props.SagaType) ??
                            throw new ApplicationException($"Couldn't find saga for Handler: {context.Props.Type}, SagaType: {context.Props.SagaType}");
 
                        var persistence = scope.GetInstance(sagaProps.SagaPersistenceType) as ISagaPersistence;
                        var handler = scope.GetInstance<IMessageHandler<T>>();

                        if (sagaProps.HasCustomHandler)
                        {
                            var customHandler = scope.GetInstance<ICustomMessageHandler<T>>();
                            var saga = customHandler as ISaga;
                            await persistence.LoadOrCreateState(saga, saga.FindKey(message, context.MetaData));
                            if (handler is ITriggeredBy<T>)
                                await handler.Handle(message, context);
                            else
                                await customHandler.Handle(message, context);
                            await persistence.AddOrUpdateState(saga);
                        }
                        else
                        {
                            var saga = handler as ISaga;
                            await persistence.LoadOrCreateState(saga, saga.FindKey(message, context.MetaData));
                            await handler.Handle(message, context);
                            await persistence.AddOrUpdateState(saga);
                        }
                    }
                    else // simple case
                    {
                        var handler = scope.GetInstance<IMessageHandler<T>>();
                        await handler.Handle(message, context);
                    }
                }
            }


        }



    }
}
   
