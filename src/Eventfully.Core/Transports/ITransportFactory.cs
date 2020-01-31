using System;
using System.Collections.Generic;
using System.Text;
using Eventfully;

namespace Eventfully.Transports
{
    public interface ITransportFactory
    {
        ITransport Create(TransportSettings settings);
    }
}
