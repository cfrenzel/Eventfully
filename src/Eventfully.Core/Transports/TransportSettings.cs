using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.Transports
{
    public interface ITransportSettings
    {
        ITransportFactory Factory { get; }
     
        ITransport Create();
    }


    public abstract class TransportSettings : ITransportSettings
    {
        public abstract ITransportFactory Factory { get; }

        public ITransport Create()
        {
            return Factory.Create(this);
        }

       
    }


  
}
