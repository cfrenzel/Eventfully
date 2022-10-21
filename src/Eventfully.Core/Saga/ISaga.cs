using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Eventfully
{
    public interface ISaga
    {
        object State { get;}
        void SetState(object state);
        object FindKey(IMessage message, MessageMetaData meta);
    }

    public interface ISaga<S, K> : ISaga
    {
        new S State { get; }
        new K FindKey(IMessage message, MessageMetaData meta);
        void SetState(S state);
    }

    public class Saga<S, K> : ISaga<S, K>
    {
        protected Dictionary<Type, Func<IMessage, MessageMetaData, K>> _keyMappers = new Dictionary<Type, Func<IMessage, MessageMetaData, K>>();


        public Saga()
        {
        }

        protected void MapIdFor<M>(Func<M, MessageMetaData, K> mapper) where M : IMessage
        {
            Func<IMessage, MessageMetaData, K> untyped = (m,md) => mapper((M)m, md);
            _keyMappers.Add(typeof(M), untyped);
        }

        public K FindKey(IMessage message, MessageMetaData meta)
        {
            return _keyMappers[message.GetType()].Invoke(message, meta);
        }

        object ISaga.FindKey(IMessage message, MessageMetaData meta) => FindKey(message, meta);
      
        public S State { get;  protected set; }
        object ISaga.State { get =>  State; }
        void ISaga.SetState(object state) {
            this.SetState((S)state);
        }

        public virtual void SetState(S state)
        {
            if (state == null)
                throw new InvalidOperationException("State can not be null");
            this.State = state;
        }

     }
}
