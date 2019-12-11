using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Eventfully.Transports.AzureServiceBus
{
    public interface IAzureServiceBusRecoverabilityProvider
    {
        Task Recover(RecoverabilityContext context);
        Task<RecoverabilityContext> OnPreHandle(RecoverabilityContext context);
     }

    public class RecoverabilityContext
    {
        public bool SkipMessage { get; set; } = false;
        public Message Message { get; set; }
        public IMessageReceiver Receiver { get; set; }
        public Endpoint Endpoint { get; set; }

        public Dictionary<string,object> TempData = new Dictionary<string, object>();

        public Message TempMessage { get; set; }
        public RecoverabilityContext(IMessageReceiver receiver, Endpoint endpoint, Message message)
        {
            Receiver = receiver;
            Endpoint = endpoint;
            Message = message;
        }
    }
}