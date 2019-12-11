using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully
{

    public interface IIntegrationCommand : IIntegrationMessage
    {
    }

    public abstract class IntegrationCommand : IntegrationMessage, IIntegrationCommand
    {
        public IntegrationCommand()
        {
        } 
    }

}
