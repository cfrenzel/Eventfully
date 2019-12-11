using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{

    public class EndpointNotFoundException : NonTransientException
    {
        public EndpointNotFoundException(string messageType) : base($"Endpoint not found for Message Type.  MessageType: {messageType}")
        {

        }
        public EndpointNotFoundException(string endpointName, string additionalMessage) : base($"Endpoint not found for Label :{endpointName}.  {additionalMessage}")
        {

        }
    }
}
