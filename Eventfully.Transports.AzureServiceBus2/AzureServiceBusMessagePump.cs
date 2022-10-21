using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Core;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eventfully.Transports.AzureServiceBus2
{

    public interface IMessagePump
    {
        Task StartAsync(CancellationToken stoppingToken);
        Task StopAsync();
    }

    public class AzureServiceBusMessagePumpSettings
    {
        public int MaxConcurrentHandlers { get; set; } = 3;
        public int MaxLevel1Retry { get; set; } = 1;
    }

    public class AzureServiceBusMessagePump : IMessagePump
    {
        private static ILogger<AzureServiceBusMessagePump> _logger = Logging.CreateLogger<AzureServiceBusMessagePump>();
        private readonly int _maxConcurrentHandlers = 3;
        private readonly int _maxLevel1Retry = 1;
        private readonly AzureServiceBusEndpoint _endpoint;
        private readonly ServiceBusClient _client;
        private readonly AsyncRetryPolicy _level1RetryPolicy;
        
        private ServiceBusProcessor _processor;
        //private readonly Func<TransportMessage, IEndpoint, Task> _handleMessageFunc;
        //private readonly IAzureServiceBusRecoverabilityProvider _recoverability;
        private readonly AzureServiceBusMetaDataMapper _metaDataMapper;
        
      
        public AzureServiceBusMessagePump(
            //Func<TransportMessage, IEndpoint, Task> handleMessageFunc,
            ServiceBusClient client,
            AzureServiceBusEndpoint endpoint,
            AzureServiceBusMetaDataMapper metaMapper = null,
            AzureServiceBusMessagePumpSettings settings = null
            //IAzureServiceBusRecoverabilityProvider recoverabilityProvider = null
            )
        {
            _client = client;
            _endpoint = endpoint;
            _metaDataMapper = metaMapper ?? new AzureServiceBusMetaDataMapper();
            //_handleMessageFunc = handleMessageFunc;
            //_recoverability = recoverabilityProvider != null ? recoverabilityProvider : new AzureServiceBusDeferRecoverabilityProvider();
            
            if(settings != null)
            {
                _maxConcurrentHandlers = settings.MaxConcurrentHandlers > 0 ? settings.MaxConcurrentHandlers : _maxConcurrentHandlers;
                _maxLevel1Retry = settings.MaxLevel1Retry >= 0 ? settings.MaxLevel1Retry : _maxLevel1Retry;
            }
            
            _level1RetryPolicy = Policy
              .Handle<Exception>()
              .RetryAsync(_maxLevel1Retry);
        }

       /*await using ServiceBusProcessor processor = client.CreateProcessor(queueName, options);

// configure the message and error handler to use
processor.ProcessMessageAsync += MessageHandler;
processor.ProcessErrorAsync += ErrorHandler;

async Task MessageHandler(ProcessMessageEventArgs args)
{
    string body = args.Message.Body.ToString();
    Console.WriteLine(body);

    // we can evaluate application logic and use that to determine how to settle the message.
    await args.CompleteMessageAsync(args.Message);
}

Task ErrorHandler(ProcessErrorEventArgs args)
{
    // the error source tells me at what point in the processing an error occurred
    Console.WriteLine(args.ErrorSource);
    // the fully qualified namespace is available
    Console.WriteLine(args.FullyQualifiedNamespace);
    // as well as the entity path
    Console.WriteLine(args.EntityPath);
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}*/
        public async Task StartAsync(CancellationToken stoppingToken)
        {
            _processor =  _endpoint.CreateProcessor(_client);
            _logger.LogInformation(" Listening on AzureServiceBus Endpoint: {EndpointName}", _endpoint.Name);

            _processor.ProcessMessageAsync += async processEventArgs =>
            {
                await _handleMessage(processEventArgs);// new RecoverabilityContext(_processor, _endpoint, message), token);
            };
            _processor.ProcessErrorAsync += args =>
            {
                _logger.LogError(args.Exception, "AzureServiceBusMessagePump encountered an unhandled exception while handling a message. Endpoint: {EndpointName}", _endpoint.Name);
                return Task.CompletedTask;  
            };

            await _processor.StartProcessingAsync(stoppingToken);
            // new MessageHandlerOptions((args) =>
            // {
            //         _logger.LogError(args.Exception, "AzureServiceBusMessagePump encountered an unhandled exception while handling a message. Endpoint: {EndpointName}", _endpoint.Name);
            //         return Task.CompletedTask;
            // })
            // {
            //     MaxConcurrentCalls = _maxConcurrentHandlers, //# concurrent handlers allowed
            //     AutoComplete = false,
            //     //MaxAutoRenewDuration = TimeSpan.FromSeconds(20)
            // });
            //

            //return Task.CompletedTask;
        }
        public async Task StopAsync()
        {
            _logger.LogInformation("AzureServiceBusMessagePump stopping. Endpoint: {EndpointName}", _endpoint.Name);
            await _processor.CloseAsync();
        }


        
        private async Task _handleMessage(ProcessMessageEventArgs @event)//RecoverabilityContext context, CancellationToken cancellationToken)
        {
            bool isHandled = false;
          
            try
            {
                ///TODO: send to Router
                Console.WriteLine("Received message");
                var message = new TransportMessage(@event.Message.Subject, @event.Message.Body.ToArray(),
                    _metaDataMapper.ExtractMetaData(@event.Message));

                //context = await _recoverability.OnPreHandle(context);
                //if (context.SkipMessage)
                //    return;

                //await _handleMessageFunc(
                //    new TransportMessage(context.Message.Label, context.Message.Body, _metaDataMapper.ExtractMetaData(context.Message)),
                //    _endpoint
                //);
                //isHandled = true;
            }
            catch (NonTransientException nte)
            {
                _logger.LogError(nte, "AzureServiceBusMessagePump encountered a NonTransient Exception handling a message. Sending to DLQ. Endpoint: {EndpointName}", _endpoint.Name);
                await @event.DeadLetterMessageAsync(@event.Message, nte.Message);
                //await _processor.DeadLetterAsync(context.Message.SystemProperties.LockToken);
            }
            catch (Exception exc)
            {
                //backoff retry
                _logger.LogError(exc, "AzureServiceBusMessagePump encountered an exception handling a message. Attempting to recover. Endpoint: {EndpointName}. Label: {messageLabel}",_endpoint.Name, @event.Message?.Subject);
                //await _recoverability.Recover(context);
            }

            //if an exception occurs completing the message then it will stay on the queue and be handled again
            //we don't want to trigger recoverability/retry
            if (isHandled)
            {
              /* await _level1RetryPolicy.ExecuteAsync(() =>
                   _processor.CompleteAsync(context.Message.SystemProperties.LockToken)
               );
                await _recoverability.OnPostHandle(context);
              */
            }
        }
        


   



    }
}