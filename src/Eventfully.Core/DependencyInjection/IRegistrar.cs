using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{
    public interface IServiceRegistrar
    {
        void AddTransient<T>();
        void AddTransient<I, T>() where T: I;

        void AddSingleton(object o);
        void AddSingleton<T>(object o);
        void AddSingleton<T>();
        void AddSingleton<I, T>(bool asBoth=false) where T : I;
    }
}
