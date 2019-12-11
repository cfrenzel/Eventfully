using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Eventfully.Handlers
{
    public interface IMessageHandlerFactory
    {
        IntegrationMessageHandlerFactoryScope CreateScope();
    }

    public static class IntegrationMessageHandlerFactoryExtensions
    {
        public static T GetInstance<T>(this IntegrationMessageHandlerFactoryScope factory)
            => (T)factory.GetInstance(typeof(T));
    }


    public abstract class IntegrationMessageHandlerFactoryScope : IDisposable
    {
        private static readonly ThreadLocal<IntegrationMessageHandlerFactoryScope> _current = new ThreadLocal<IntegrationMessageHandlerFactoryScope>(trackAllValues: false);

        protected IntegrationMessageHandlerFactoryScope()
        {
            _current.Value = this;
        }

        public static IntegrationMessageHandlerFactoryScope Current => _current.Value;

        public abstract object GetInstance(Type type);


        public virtual void DisposeScope() { }

        public void Dispose()
        {
            try
            {
                DisposeScope();
            }
            finally
            {
                _current.Value = null;
            }
        }
    }
}
