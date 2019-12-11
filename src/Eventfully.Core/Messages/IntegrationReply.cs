using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{

    public interface IIntegrationReply : IIntegrationMessage
    {
    }

    public abstract class IntegrationReply : IntegrationMessage, IIntegrationReply
    {
        public IntegrationReply() {}

    }

}
