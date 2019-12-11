using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Eventfully.Outboxing;

namespace Eventfully
{
    public class MicrosoftDependencyInjectionOutboxFactory : IOutboxFactory
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public MicrosoftDependencyInjectionOutboxFactory(IServiceScopeFactory serviceScopeFactory)
        {
            if (serviceScopeFactory == null)
                throw new ArgumentNullException(nameof(serviceScopeFactory));
            _serviceScopeFactory = serviceScopeFactory;
        }

        public MicrosoftDependencyInjectionOutboxScope CreateScope()
        {
            return new MicrosoftDependencyInjectionOutboxScope(_serviceScopeFactory.CreateScope());
        }
    }

    public class MicrosoftDependencyInjectionOutboxScope : OutboxFactoryScope
    {
        private readonly IServiceScope _serviceScope;

        public MicrosoftDependencyInjectionOutboxScope(IServiceScope serviceScope)
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
 