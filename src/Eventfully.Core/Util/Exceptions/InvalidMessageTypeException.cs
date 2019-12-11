using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{

    public class InvalidMessageTypeException : NonTransientException
    {
        public InvalidMessageTypeException(string messageType):base($"Invalid Message Type.  MessageType: {messageType}")
        {

        }
    }
}
