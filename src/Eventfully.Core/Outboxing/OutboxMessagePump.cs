using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Eventfully.Outboxing
{
   
    public class OutboxMessagePump : IDisposable
    {
        private static ILogger<OutboxMessagePump> _log = Logging.CreateLogger<OutboxMessagePump>();

        private readonly  MessagingService _messagingService;
       
        private Timer _dispatchTimer;
        private TimeSpan _dispatchFrequency = TimeSpan.FromSeconds(3);//time between processing outbox
       
        private Timer _cleanUpTimer;
        private TimeSpan _cleanupFrequency = TimeSpan.FromHours(1);//time between running cleanup on outbox

        private Timer _resetTimer;
        private TimeSpan _resetFrequency = TimeSpan.FromSeconds(30);//time between running reset on stale pending events

        private TimeSpan _cleanupAge = TimeSpan.FromMinutes(60);
        private TimeSpan _resetAge = TimeSpan.FromMinutes(1);

        private readonly int _maxConcurrency;
        public OutboxMessagePump(MessagingService messagingService, int maxConcurrency = 1)
        {
            _messagingService = messagingService;
            _maxConcurrency = maxConcurrency;
        }
      
        public Task StartAsync(CancellationToken stoppingToken)
        {
            _log.LogInformation("OutboxMessagePump running.");

            _dispatchTimer = new Timer(ProcessMessages, null, TimeSpan.FromSeconds(15), _dispatchFrequency);

            //reset old pending events from the outbox
            _resetTimer = new Timer(Reset, null, TimeSpan.FromMinutes(2), _resetFrequency);

            //delete old events from the outbox
            _cleanUpTimer = new Timer(CleanUp, null, TimeSpan.FromMinutes(3), _cleanupFrequency);

            return Task.CompletedTask;
        }

        private async void ProcessMessages(object state)
        {
            var dispatchDelay = _dispatchFrequency;
            try
            {
                //stop and restart after we process
                _dispatchTimer.Change(Timeout.Infinite, Timeout.Infinite);
              
                var res = await _messagingService.RelayOutbox();
                if (res.MessageCount >= res.MessageCount)
                    dispatchDelay = TimeSpan.FromSeconds(0);

                _log.LogDebug("OutboxMessagePumpService is processing.");
            }
            catch (Exception exc)
            {
                _log.LogError(exc, "Exception Processing Outbox Messages.");
            }
            finally
            {
                _dispatchTimer.Change(dispatchDelay, _dispatchFrequency);
            }
        }

        private async void Reset(object state)
        {
            try
            {
                await _messagingService.ResetOutbox(_resetAge);
                _log.LogDebug("OutboxMessagePumpService is Resetting. ExecutionCount");

            }
            catch (Exception exc)
            {
                _log.LogError(exc, "Exception Resetting Outbox");
            }
        }

        private async void CleanUp(object state)
        {
            try
            {
                await _messagingService.CleanUpOutbox(_cleanupAge);
                _log.LogDebug("OutboxMessagePump is Cleaning Up.");
            }
            catch (Exception exc)
            {
                _log.LogError(exc, "Exception Cleaning up Outbox");
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _dispatchTimer?.Change(Timeout.Infinite, 0);
            _cleanUpTimer?.Change(Timeout.Infinite, 0);
            _resetTimer?.Change(Timeout.Infinite, 0);
            _log.LogInformation("OutboxMessagePump stopping.");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _dispatchTimer?.Dispose();
            _cleanUpTimer?.Dispose();
            _resetTimer?.Dispose();
        }
    }
}
