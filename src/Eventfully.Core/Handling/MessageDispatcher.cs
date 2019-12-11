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
        private readonly IMessageHandlerFactory _handlerFactory;

        public MessageDispatcher(IMessageHandlerFactory handlerFactory)
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
            public abstract Task Handle(IIntegrationMessage message, MessageContext context, IMessageHandlerFactory handlerFactory);
        }


        private class IntegrationMessageDispatcherHandler<T> : IntegrationMessageDispatcherHandler
        where T : IIntegrationMessage
        {
            public override Task Handle(IIntegrationMessage message, MessageContext context,  IMessageHandlerFactory handlerFactory)
            {
                return HandleCore((T)message, context, handlerFactory);
            }

            private static async Task HandleCore(T message, MessageContext context, IMessageHandlerFactory handlerFactory)
            {
                using (var scope = handlerFactory.CreateScope())
                {
                    var handler = scope.GetInstance<IMessageHandler<T>>();
                    var outboxSession = scope.GetInstance<IOutboxSession>();

                    ///TODO: we shouldn't be getting the outbox from the handlerFactory
                    ///and we should be setting it through the constructor
                    ///we need merge the Factories 
                    context.OutboxSession = outboxSession;

                    if (context.Props != null && context.Props.HasSagaHandler)
                    {
                        var sagaProps = MessagingMap.GetSagaProps(context.Props.SagaType);
                        if (sagaProps == null)
                            throw new ApplicationException($"Couldn't find saga for Handler: {handler.GetType()}, SagaType: {context.Props.SagaType}");

                        var saga = handler as ISaga;
                        var persistence = scope.GetInstance(sagaProps.SagaPersistenceType) as ISagaPersistence;
                        await persistence.LoadState(saga, saga.FindKey(message, context.MetaData));
                        await handler.Handle(message, context);
                        await persistence.SaveState(saga);
                    }
                    else
                        await handler.Handle(message, context);
                }
            }


        }
    }


}
   
