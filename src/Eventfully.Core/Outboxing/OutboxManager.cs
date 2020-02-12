using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Eventfully.Outboxing
{
    public class OutboxManager : IOutboxManager
    {
        private static ILogger<OutboxManager> _log = Logging.CreateLogger<OutboxManager>();

        private readonly IOutbox _outbox;
       
        private Dispatcher _dispatcher;
        private Timer _dispatchTimer;
        private TimeSpan _dispatchFrequency = TimeSpan.FromSeconds(3);//time between processing outbox

        private Timer _cleanUpTimer;
        private TimeSpan _cleanupFrequency = TimeSpan.FromHours(1);//time between running cleanup on outbox
        private TimeSpan _cleanupAge = TimeSpan.FromMinutes(60);

        private Timer _resetTimer;
        private TimeSpan _resetFrequency = TimeSpan.FromSeconds(30);//time between running reset on stale pending events
        private TimeSpan _resetAge = TimeSpan.FromMinutes(1);

        private ICountingSemaphore _outboxConsumerSemaphore;
        private Timer _renewSemaphoreTimer; //limits the conccurent number of OutboxManagers
        private TimeSpan _renewSemaphoreFrequency;

        private readonly string _uniqueIdentifier;

        private readonly int _maxConcurrency;
        private bool _hasOwnership = true;

        public OutboxManager(IOutbox outbox,
            Dispatcher dispatcher, 
            int maxConcurrency = 1, 
            ICountingSemaphore outboxConsumerSemaphore = null,
            string uniqueIdentifier = null)
        {
            _dispatcher = dispatcher;
            _outbox = outbox;
            _maxConcurrency = maxConcurrency;
            _outboxConsumerSemaphore = outboxConsumerSemaphore;
            _uniqueIdentifier = !String.IsNullOrEmpty(uniqueIdentifier) ? uniqueIdentifier : Guid.NewGuid().ToString();
               
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            _log.LogInformation("OutboxMessagePump running.");

            if (_outboxConsumerSemaphore != null)
            {
                _hasOwnership = false;
                var periodInSeconds = Convert.ToInt32(Math.Floor(_outboxConsumerSemaphore.TimeoutInSeconds * 0.8));
                _renewSemaphoreFrequency = TimeSpan.FromSeconds(periodInSeconds < 2 ? 2 : periodInSeconds);
                _renewSemaphoreTimer = new Timer(Renew, null, TimeSpan.FromSeconds(0), _renewSemaphoreFrequency);
                _dispatchTimer = new Timer(ProcessMessages, null, Timeout.Infinite, Timeout.Infinite);
            }
            else
            {
                _dispatchTimer = new Timer(ProcessMessages, null, TimeSpan.FromSeconds(5), _dispatchFrequency);
            }

            //reset old pending messages from the outbox
            _resetTimer = new Timer(Reset, null, TimeSpan.FromMinutes(2), _resetFrequency);

            //delete old processed messages from the outbox
            _cleanUpTimer = new Timer(CleanUp, null, TimeSpan.FromMinutes(3), _cleanupFrequency);

            await _outbox.StartAsync(_dispatcher);
        }

        public async Task StopAsync()
        {
            _renewSemaphoreTimer?.Change(Timeout.Infinite, 0);
            _renewSemaphoreTimer = null;
           
            _dispatchTimer?.Change(Timeout.Infinite, 0);
            _dispatchTimer = null;
            
            _cleanUpTimer?.Change(Timeout.Infinite, 0);
            _cleanUpTimer = null;
            
            _resetTimer?.Change(Timeout.Infinite, 0);
            _resetTimer = null;
            
            _log.LogInformation("OutboxManager stopping.");
            await _outbox.StopAsync();

            if (_outboxConsumerSemaphore != null)
                await _outboxConsumerSemaphore.TryRelease(this._uniqueIdentifier);
        }

       
        public void Dispose()
        {
            _dispatchTimer?.Dispose();
            _cleanUpTimer?.Dispose();
            _resetTimer?.Dispose();
            _renewSemaphoreTimer?.Dispose();
        }

        private async void ProcessMessages(object state)
        {
            if (!_hasOwnership)
                return;

            var dispatchDelay = _dispatchFrequency;
            try
            {
                //stop and restart after we process
                _dispatchTimer.Change(Timeout.Infinite, Timeout.Infinite);

                //var res = await _messagingService.RelayOutbox();
                var res = await _outbox.Relay(_dispatcher);
                if (res.MessageCount >= res.MaxMessageCount)
                    dispatchDelay = TimeSpan.FromSeconds(0);///TODO: with multiple consumers this shouldn't be necessary

                _log.LogDebug("OutboxMessagePumpService is processing.");
            }
            catch (Exception exc)
            {
                _log.LogError(exc, "Exception Processing Outbox Messages.");
            }
            finally
            {
                if(_dispatchTimer != null)//if not stopping
                    _dispatchTimer.Change(dispatchDelay, _dispatchFrequency);
            }
        }

        private async void Reset(object state)
        {
            if (!_hasOwnership)
                return;

            try
            {
                if (_resetTimer == null)//if stopping
                    return;
                await _outbox.Reset(_resetAge);
                _log.LogDebug("OutboxMessagePumpService is Resetting.");
            }
            catch (Exception exc)
            {
                _log.LogError(exc, "Exception Resetting Outbox");
            }
        }

        private async void CleanUp(object state)
        {
            if (!_hasOwnership)
                return;

            try
            {
                if (_cleanUpTimer == null)//if stopping
                    return;

                await _outbox.CleanUp(_cleanupAge);
                _log.LogDebug("OutboxMessagePump is Cleaning Up.");
            }
            catch (Exception exc)
            {
                _log.LogError(exc, "Exception Cleaning up Outbox");
            }
        }

        /// <summary>
        /// If we're using a distributed semaphore to control access to the outbox
        /// this will periodically renew the semaphore
        /// </summary>
        /// <param name="state"></param>
        private async void Renew(object state)
        {
            try
            {
                if (_renewSemaphoreTimer == null)//if stopping
                    return;

                _renewSemaphoreTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _hasOwnership = await _outboxConsumerSemaphore.TryRenew(this._uniqueIdentifier);
                if (_hasOwnership)
                   _dispatchTimer.Change(TimeSpan.FromSeconds(0), _dispatchFrequency);
                else
                    _dispatchTimer.Change(Timeout.Infinite, Timeout.Infinite);

                _log.LogDebug("OutboxMessagePumpService is Renewing Semaphore: {SemaphoreName}.  Renew Result: {result}", _outboxConsumerSemaphore.Name, _hasOwnership);
            }
            catch (Exception exc)
            {
                _log.LogError(exc, "Exception Resetting Outbox");
            }
            finally
            {
                if (_renewSemaphoreTimer != null)//if not stopping
                    _renewSemaphoreTimer.Change(_renewSemaphoreFrequency, _renewSemaphoreFrequency);
            }
        }

        
    }
}
