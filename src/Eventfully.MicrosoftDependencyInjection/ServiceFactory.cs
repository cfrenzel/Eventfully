using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Eventfully
{
    public class MicrosoftDependencyInjectionServiceFactory : IServiceFactory
    {
        private readonly IServiceProvider _provider;
        private readonly IServiceScope _scope;
        public MicrosoftDependencyInjectionServiceFactory(IServiceProvider provider) => _provider = provider;

        private MicrosoftDependencyInjectionServiceFactory(IServiceScope scope) {
            _scope = scope;
            _provider = _scope.ServiceProvider;
        }
        public object GetInstance(Type type) => _provider.GetRequiredService(type);

        public IServiceFactory CreateScope() => new MicrosoftDependencyInjectionServiceFactory(_provider.CreateScope());

        public void Dispose()
        {
            if (_scope != null)
                _scope.Dispose();
        }       
    }
}
 