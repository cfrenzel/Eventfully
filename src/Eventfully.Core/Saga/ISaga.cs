using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Eventfully
{

    public interface ISaga
    {
        object State { get; set; }
        object FindKey(IIntegrationMessage message, MessageMetaData meta);
    }

    public interface ISaga<T, K> : ISaga
    {
        new T State { set; }
        new K FindKey(IIntegrationMessage message, MessageMetaData meta);
    }



    public class Saga<T, K> : ISaga<T, K>
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
      
        public T State { protected get;  set; }
        object ISaga.State { get =>  State; set => State = (T)value; }

     }


}
