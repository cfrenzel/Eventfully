using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{
    public class ServiceRegistrar : IServiceRegistrar
    {
        private IServiceCollection _services;
        public ServiceRegistrar(IServiceCollection services) => _services = services;

        public void AddSingleton(object o) => _services.AddSingleton(o);
        public void AddSingleton<T>(object o) => _services.AddSingleton(typeof(T), o);
        public void AddSingleton<T>() => _services.AddSingleton(typeof(T));
        public void AddSingleton<I, T>(bool asBoth = false) where T : I
        {
            if(asBoth)
            {
                _services.AddSingleton(typeof(T));
                _services.AddSingleton(typeof(I), x =>
                    x.GetRequiredService<T>()
                );
            }
            else
                _services.AddSingleton(typeof(I), typeof(T));
        }
        public void AddTransient<T>() => _services.AddTransient(typeof(T));
        public void AddTransient<I, T>() where T: I => _services.AddTransient(typeof(I), typeof(T));

    }
}
