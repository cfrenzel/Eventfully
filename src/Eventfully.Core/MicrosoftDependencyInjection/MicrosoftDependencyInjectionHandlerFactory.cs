using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Eventfully.Handlers;

namespace Eventfully
{
    public class MicrosoftDependencyInjectionHandlerFactory : IMessageHandlerFactory
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public MicrosoftDependencyInjectionHandlerFactory(IServiceScopeFactory serviceScopeFactory)
        {
            if (serviceScopeFactory == null)
                throw new ArgumentNullException(nameof(serviceScopeFactory));
            _serviceScopeFactory = serviceScopeFactory;
        }

        public IntegrationMessageHandlerFactoryScope CreateScope()
        {
            return new MicrosoftDependencyInjectionHandlerScope(_serviceScopeFactory.CreateScope());
        }
    }


    internal class MicrosoftDependencyInjectionHandlerScope : IntegrationMessageHandlerFactoryScope
    {
        private readonly IServiceScope _serviceScope;

        public MicrosoftDependencyInjectionHandlerScope(IServiceScope serviceScope)
        {
            if (serviceScope == null) throw new ArgumentNullException(nameof(serviceScope));
            _serviceScope = serviceScope;
        }

        public override object GetInstance(Type type)
        {
            return ActivatorUtilities.GetServiceOrCreateInstance(_serviceScope.ServiceProvider, type);
        }

        public override void DisposeScope()
        {
            _serviceScope.Dispose();
        }
    }

}
 