using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{
    public interface IEvent : IMessage
    {
    }

    public abstract class Event : Message, IEvent
    {
    }

}
