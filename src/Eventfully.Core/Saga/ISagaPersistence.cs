using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Eventfully
{
  
    public interface ISagaPersistence
    {
        Task LoadOrCreateState(ISaga saga, object sagaId);
        Task AddOrUpdateState(ISaga saga);
    }

    public interface ISagaPersistence<T, K> : ISagaPersistence
    {
        Task LoadOrCreateState(ISaga<T,K> saga, K sagaId);
        Task AddOrUpdateState(ISaga<T,K> saga);
    }

    public abstract class SagaPersistence<T, K> : ISagaPersistence<T, K>
    {
        public abstract Task AddOrUpdateState(ISaga<T,K> saga);

        public abstract Task LoadOrCreateState(ISaga<T,K> saga, K sagaId);

        Task ISagaPersistence.LoadOrCreateState(ISaga saga, object sagaId) => LoadOrCreateState((ISaga<T,K>) saga, (K) sagaId);
        
        Task ISagaPersistence.AddOrUpdateState(ISaga saga) => AddOrUpdateState((ISaga<T, K>)saga);
    }

}
