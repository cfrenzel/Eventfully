using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{

    public class MessageExpiredException : NonTransientException
    {
        public MessageExpiredException(string messageType):base($"Message Expired.  MessageType: {messageType}")
        {

        }
    }
}
