using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{
    public interface IMessageExtractor
    {
        IMessage Extract(byte[] data);
    }
}
