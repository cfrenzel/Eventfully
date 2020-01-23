using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Eventfully.Handlers;
using Eventfully.Outboxing;

namespace Eventfully
{

   
    public static class DependencyInjectionExtensions
    {
        public static IServiceRegistrar AddMessaging(this IServiceCollection services,
               Profile profile,
               params Assembly[] messageAndHandlerAssemblies)
        {
            return AddMessaging(services, profile, null, messageAndHandlerAssemblies);
        }

        public static IServiceRegistrar AddMessaging(this IServiceCollection services, 
            Profile profile,
            EndpointBindings bindings,
            params Assembly[] messageAndHandlerAssemblies)
        {
            MessagingService.InitializeTypes(messageAndHandlerAssemblies);
            if(bindings != null)
                profile.AddBindings(bindings);

            services.AddTransient<IServiceFactory>(x=> new MicrosoftDependencyInjectionServiceFactory(x));            
            services.AddSingleton(profile);
            services.AddSingleton<MessagingService>();
            services.AddTransient<IMessagingClient, MessagingClient>();

            //setup user message handlers
            services.Scan(c =>
            {
                c.FromAssemblies(messageAndHandlerAssemblies)
                    .AddClasses(t => t.AssignableTo(typeof(IMessageHandler<>)))
                        .AsImplementedInterfaces()
                        .WithTransientLifetime()
                    .AddClasses(t => t.AssignableTo(typeof(ISaga<,>)))
                        .AsImplementedInterfaces()
                        .WithTransientLifetime()
                     .AddClasses(t => t.AssignableTo(typeof(ISagaPersistence<,>)))
                        .AsImplementedInterfaces()
                        .WithTransientLifetime()
                     ;
            });

            return new ServiceRegistrar(services);
        }
        
        //public static void UseMessagingHost(this IApplicationBuilder builder)
        //{
        //    UseMessagingHost(builder.ApplicationServices);
        //}

        /// <summary>
        /// Helper for applications other than asp.net core where you might not have access to an IApplicationBuilder
        /// </summary>
        /// <param name="provider"></param>
        public static void UseMessagingHost(this IServiceProvider provider)
        {
            var messagingService = provider.GetRequiredService<MessagingService>();
            Logging.LoggerFactory = provider.GetRequiredService<ILoggerFactory>();
            messagingService.StartAsync().ConfigureAwait(false).GetAwaiter();
        }
    }

}
