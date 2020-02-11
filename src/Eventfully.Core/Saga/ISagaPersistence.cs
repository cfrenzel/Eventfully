using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Eventfully
{
  
    public interface ISagaPersistence
    {
        Task LoadState(ISaga saga, object sagaId);
        Task SaveState(ISaga saga);
    }

    public interface ISagaPersistence<T, K> : ISagaPersistence
    {
        Task LoadState(ISaga<T,K> saga, K sagaId);
        Task SaveState(ISaga<T,K> saga);
    }

    public abstract class SagaPersistece<T, K> : ISagaPersistence<T, K>
    {
        public abstract Task SaveState(ISaga<T,K> saga);

        public abstract Task LoadState(ISaga<T,K> saga, K sagaId);

        Task ISagaPersistence.LoadState(ISaga saga, object sagaId) => LoadState((ISaga<T,K>) saga, (K) sagaId);
        
        Task ISagaPersistence.SaveState(ISaga saga) => SaveState((ISaga<T, K>)saga);
    }

}
