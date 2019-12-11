using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{

    public class UnknownMessageTypeException : NonTransientException
    {
        public UnknownMessageTypeException(string messageType):base($"Unknown Message Type.  MessageType: {messageType}")
        {

        }
    }
}
