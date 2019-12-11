using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Eventfully.Handlers
{
    public interface IMessageDispatcher
    {
        Task Dispatch(IIntegrationMessage message, MessageContext context);
    }
}
