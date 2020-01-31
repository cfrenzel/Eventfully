using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eventfully.Transports.AzureServiceBus
{

    public interface IMessagePump
    {
        Task StartAsync(CancellationToken stoppingToken);
        Task StopAsync();

    }

    public class AzureServiceBusMessagePumpSettings
    {
        public int MaxConcurrentHandlers { get; set; } = 3;
        public int MaxCompletionImmediateRetry { get; set; } = 1;
    }

    public class AzureServiceBusMessagePump : IMessagePump
    {
        private static ILogger<AzureServiceBusMessagePump> _logger = Logging.CreateLogger<AzureServiceBusMessagePump>();

        private readonly Func<TransportMessage, IEndpoint, Task> _handleMessageFunc;
        private readonly IAzureServiceBusRecoverabilityProvider _recoverability;
        private readonly AzureServiceBusMetaDataMapper _metaDataMapper;
        private readonly IEndpoint _endpoint;
        private readonly AsyncRetryPolicy _completeImmediateRetryPolicy;

        private IMessageReceiver _receiver;
        private int _maxConcurrentHandlers = 3;
        private int _maxCompletionImmediateRetry = 1;


        public AzureServiceBusMessagePump(
            Func<TransportMessage, IEndpoint, Task> handleMessageFunc,
            IEndpoint endpoint,
            AzureServiceBusMetaDataMapper metaMapper = null,
            AzureServiceBusMessagePumpSettings settings = null,
            IAzureServiceBusRecoverabilityProvider recoverabilityProvider = null
            )
        {
            _endpoint = endpoint;
            _handleMessageFunc = handleMessageFunc;
            _metaDataMapper = metaMapper ?? new AzureServiceBusMetaDataMapper();
            _recoverability = recoverabilityProvider != null ? recoverabilityProvider : new AzureServiceBusDeferRecoverabilityProvider();
            
            if(settings != null)
            {
                _maxConcurrentHandlers = settings.MaxConcurrentHandlers > 0 ? settings.MaxConcurrentHandlers : _maxConcurrentHandlers;
                _maxCompletionImmediateRetry = settings.MaxCompletionImmediateRetry > 0 ? settings.MaxCompletionImmediateRetry : _maxCompletionImmediateRetry;
            }
            _completeImmediateRetryPolicy = Policy
              .Handle<Exception>()
              .RetryAsync(_maxCompletionImmediateRetry);
        }

       
        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AzureServiceBusMessagePump running. Endpoint: {EndpointName}", _endpoint.Name);

            if (!_endpoint.Settings.IsReader)
                return Task.CompletedTask;

            _receiver = AzureServiceBusClientCache.GetReceiver(_endpoint.Settings.ConnectionString);
            _receiver.RegisterMessageHandler(async (message, token) =>
            {
                if (token.IsCancellationRequested)
                    return;

                await _handleMessage(new RecoverabilityContext(_receiver, _endpoint, message), token);
            },
            new MessageHandlerOptions((args) =>
            {
                    _logger.LogError(args.Exception, "AzureServiceBusMessagePump encountered an unhandled exception while handling a message. Endpoint: {EndpointName}", _endpoint.Name);
                    return Task.CompletedTask;
            })
            {
                MaxConcurrentCalls = _maxConcurrentHandlers, //# concurrent handlers allowed
                AutoComplete = false,
                //MaxAutoRenewDuration = TimeSpan.FromSeconds(20)
            });

            return Task.CompletedTask;
        }
        public Task StopAsync()
        {
            _logger.LogInformation("AzureServiceBusMessagePump stopping. Endpoint: {EndpointName}", _endpoint.Name);
            return AzureServiceBusClientCache.CloseReceiver(_receiver);
        }


        private async Task _handleMessage(RecoverabilityContext context, CancellationToken cancellationToken)
        {
            bool isHandled = false;
          
            try
            {
                context = await _recoverability.OnPreHandle(context);
                if (context.SkipMessage)
                    return;

                await _handleMessageFunc(
                    new TransportMessage(context.Message.Label, context.Message.Body, _metaDataMapper.ExtractMetaData(context.Message)),
                    _endpoint
                );
                isHandled = true;
            }
            catch (NonTransientException nte)
            {
                _logger.LogError(nte, "AzureServiceBusMessagePump encountered a NonTransient Exception handling a message. Sending to DLQ. Endpoint: {EndpointName}", context.Endpoint.Name);
                await _receiver.DeadLetterAsync(context.Message.SystemProperties.LockToken);
            }
            catch (Exception exc)
            {
                //backoff retry
                _logger.LogError(exc, "AzureServiceBusMessagePump encountered an exception handling a message. Attempting to recover. Endpoint: {EndpointName}. Label: {messageLabel}", context.Endpoint.Name, context.Message?.Label);
                await _recoverability.Recover(context);
            }

            //if an exception occurs completing the message then it will stay on the queue and be handled again
            //we don't want to trigger recoverability/retry
            if (isHandled)
            {
               await _completeImmediateRetryPolicy.ExecuteAsync(() =>
                   _receiver.CompleteAsync(context.Message.SystemProperties.LockToken)
               );
                await _recoverability.OnPostHandle(context);

            }
        }


   



    }
}
