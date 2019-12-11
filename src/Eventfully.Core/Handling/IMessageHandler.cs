using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eventfully
{
    public interface IMessageHandler<T>
    {
        Task Handle(T message, MessageContext context);
    }
}
