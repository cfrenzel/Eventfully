using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{
    public interface IServiceFactory: IDisposable
    {
        object GetInstance(Type type);
        IServiceFactory CreateScope();
    }

    public static class ServiceFactoryExtension
    {
        public static T GetInstance<T>(this IServiceFactory factory)
        {
            return (T)factory.GetInstance(typeof(T));
        }
    }
}