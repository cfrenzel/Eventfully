using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Eventfully.Transports;
using Microsoft.Extensions.Hosting;

namespace Eventfully
{
    public interface IServiceFactory: IDisposable
    {
        object GetInstance(Type type);
        IServiceFactory CreateScope();
    }

    public static class DependencyInjectionHelpers
    {
        public static T GetInstance<T>(this IServiceFactory factory)
        {
            return (T)factory.GetInstance(typeof(T));
        }

        /// <summary>
        /// Helper to add all the base dependencies for messaging to
        /// the DI container regardless of implementation
        /// Container integrations need to call this to bootstrap the system 
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="configBuilder"></param>
        public static void AddMessaging(this IServiceRegistrar reg,  Action<MessagingConfiguration> configBuilder = null)
        {
            var config = new MessagingConfiguration(reg);
            if (configBuilder != null)
                configBuilder.Invoke(config);

            reg.AddSingleton(config);
            reg.AddSingleton<MessagingService>();
            reg.AddSingleton<ServiceBusManager>();
            reg.AddTransient<IMessagingClient, MessagingClient>();
            reg.AddSingleton<IHostedService, ServiceBusManager>();
        }

        
        public static void InitializeTypes(params Assembly[] assemblies)
        {
            var messageInterface = typeof(IMessage);
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
                        //register a typed message
                        MessagingMap2.AddMessageType(type);
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
                                                      
                            MessagingMap2.AddSaga(type, sagaIdType, sagaStateType, sagaPersistenceType, handledMessageTypes, hasCustomHandler);
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
}