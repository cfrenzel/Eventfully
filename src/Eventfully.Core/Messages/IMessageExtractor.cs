using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{
    public interface IMessageExtractor
    {
        IIntegrationMessage Extract(byte[] data);
    }
}
