using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eventfully
{
    /// <summary>
    /// Alias for IMessageHandler used to set a message that starts a saga/process manager
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ITriggeredBy<T> : IMessageHandler<T> { }
 
}
