using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.Transports
{
    public interface ITransportFactory
    {
        Transport Create(TransportSettings settings);
    }
}
