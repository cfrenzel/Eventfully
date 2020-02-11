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
        object FindKey(IIntegrationMessage message, MessageMetaData meta);
    }

    public interface ISaga<S, K> : ISaga
    {
        new S State { get; }
        new K FindKey(IIntegrationMessage message, MessageMetaData meta);
        void SetState(S state);
    }

    public class Saga<S, K> : ISaga<S, K>
    {
        protected Dictionary<Type, Func<IIntegrationMessage, MessageMetaData, K>> _keyMappers = new Dictionary<Type, Func<IIntegrationMessage, MessageMetaData, K>>();
        
        protected void MapIdFor<M>(Func<M, MessageMetaData, K> mapper) where M : IIntegrationMessage
        {
            Func<IIntegrationMessage, MessageMetaData, K> untyped = (m,md) => mapper((M)m, md);
            _keyMappers.Add(typeof(M), untyped);
        }

        public K FindKey(IIntegrationMessage message, MessageMetaData meta)
        {
            return _keyMappers[message.GetType()].Invoke(message, meta);
        }

        object ISaga.FindKey(IIntegrationMessage message, MessageMetaData meta) => FindKey(message, meta);
      
        public S State { get;  protected set; }
        object ISaga.State { get =>  State; }
        void ISaga.SetState(object state) {
            if (state == null)
                SetState((S)Activator.CreateInstance(typeof(S)));
            //throw new ArgumentNullException("SetState requires a State object.  State cannot be null");
            this.SetState((S)state);
        }

        public virtual void SetState(S state)
        {
            this.State = state;
        }

     }
}
