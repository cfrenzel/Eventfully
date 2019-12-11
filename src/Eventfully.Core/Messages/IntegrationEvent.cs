using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{
    public interface IIntegrationEvent : IIntegrationMessage
    {
    }

    public abstract class IntegrationEvent : IntegrationMessage, IIntegrationEvent
    {
    }

}
