using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Eventfully
{
    /// <summary>
    /// Allows registering as a handler for a message type while
    /// processing each message type through a single Handle method
    /// used by the ProcessManagerStateMachine
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICustomMessageHandler<T>
    {
        Task Handle(IMessage message, MessageContext context);
    }
}
