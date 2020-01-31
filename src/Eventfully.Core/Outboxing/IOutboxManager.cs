using System;
using System.Threading;
using System.Threading.Tasks;

namespace Eventfully.Outboxing
{
    public interface IOutboxManager : IDisposable
    {
        Task StartAsync(CancellationToken stoppingToken);

        Task StopAsync();
    }
}