using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Eventfully.Outboxing
{
    public interface IOutboxFactory
    {
        MicrosoftDependencyInjectionOutboxScope CreateScope();
    }

    public static class OutboxFactoryExtensions
    {
        public static T GetInstance<T>(this OutboxFactoryScope factory)
            => (T)factory.GetInstance(typeof(T));
    }


    public abstract class OutboxFactoryScope : IDisposable
    {
        private static readonly ThreadLocal<OutboxFactoryScope> _current = new ThreadLocal<OutboxFactoryScope>(trackAllValues: false);

        protected OutboxFactoryScope()
        {
            _current.Value = this;
        }

        public static OutboxFactoryScope Current => _current.Value;

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
