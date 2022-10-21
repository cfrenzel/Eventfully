using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Eventfully.Transports
{
    /// <summary>
    /// We use the Transport itself to manage consumption from the bus (reading from queues, subscriptions, etc...)
    /// The ServiceBusManager creates a background process to coordinate the starting
    /// and stopping of the Transports
    /// </summary>
    public class ServiceBusManager : IHostedService 
    {
        private readonly IEnumerable<ITransport2> _transports;
        private readonly IServiceFactory _services;
        
        public ServiceBusManager(IEnumerable<ITransport2> transports, IServiceFactory service, ILoggerFactory logger)
        {
            Logging.LoggerFactory = logger;
            _transports = transports;
            _services = _services;
        }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            //Logging.LoggerFactory = _services.GetInstance<ILoggerFactory>();
            foreach (var transport in _transports)
            {
                //register the endpoints configured on the transport
                //foreach (var endpoint in transport.GetEndpoints())
                //    MessagingMap2.AddEndpoint(endpoint.Name, endpoint);
                await transport.StartAsync(cancellationToken);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var transport in _transports)
            {
                await transport.StopAsync();
            }
        }
    }
}