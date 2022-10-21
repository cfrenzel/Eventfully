using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eventfully.Transports.AzureServieBus.IntegrationTests
{
    public interface ITestMessageHandler
    {
        Task HandleException(ExceptionReceivedEventArgs args);
        Task HandleMessage(Microsoft.Azure.ServiceBus.Message m, CancellationToken token);

    }

    public interface ITestPumpCallback
    {
        Task Handle(TransportMessage message, IEndpoint endpoint);
    }

}
