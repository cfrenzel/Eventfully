using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Eventfully.Transports.AzureServiceBus
{

    public class AzureServiceBusCloneRecoverabilityProvider : IAzureServiceBusRecoverabilityProvider
    {
        private readonly AsyncRetryPolicy _completeImmediateRetryPolicy;
        private int _maxCompletionImmediateRetry = 1;
        private IRetryIntervalStrategy _retryStrategy;
        private int _maxDeliveryCount = 20;


        public AzureServiceBusCloneRecoverabilityProvider(IRetryIntervalStrategy retryStrategy = null, int maxDeliveryCount = 20)
        {
            _completeImmediateRetryPolicy = Policy
                .Handle<Exception>()
                .RetryAsync(_maxCompletionImmediateRetry);

            _retryStrategy = retryStrategy != null ? retryStrategy : new DefaultExponentialRetryStrategy();
            _maxDeliveryCount = maxDeliveryCount;
            
        }

        public async Task<RecoverabilityContext> OnPreHandle(RecoverabilityContext context)
        {
            var count = _getRecoveryCount(context.Message, context);
            if (await _handleMaxRetry(count, context.Message, context))
                context.SkipMessage = true;
            return context;
        }

      

        public async Task Recover(RecoverabilityContext context)
        {
            var sender = AzureServiceBusClientCache.GetSender(context.Endpoint.Settings.ConnectionString);
            var count = _getRecoveryCount(context.Message, context);

            var retryMessage = context.Message.Clone();
            retryMessage.MessageId = Guid.NewGuid().ToString();
            retryMessage.UserProperties.Add("OriginalMessageId", context.Message.MessageId);
            retryMessage.UserProperties.Add("RecoveryCount", ++count);

            await sender.ScheduleMessageAsync(
               retryMessage,
               this._retryStrategy.GetNextDateUtc(++count)
            );

            //complete the original message - we've already scheduled a clone
            await _completeImmediateRetryPolicy.ExecuteAsync(() =>
                 context.Receiver.CompleteAsync(context.Message.SystemProperties.LockToken)
            );
        }

        public  Task OnPostHandle(RecoverabilityContext context)// int timesQueued, Endpoint endpoint, IMessageReceiver receiver, Message controlMessage = null, string description = null, Exception exc = null)
        {
            //no cleanup to do here
            return Task.CompletedTask;
        }

        private async Task<bool> _handleMaxRetry(int recoveryCount, Microsoft.Azure.ServiceBus.Message message, RecoverabilityContext context)
        {
            if (message.SystemProperties.DeliveryCount > 1 || recoveryCount > _maxDeliveryCount)
            {
                await context.Receiver.DeadLetterAsync(message.SystemProperties.LockToken);
                return true;
            }
            return false;
        }

        private int _getRecoveryCount(Microsoft.Azure.ServiceBus.Message message, RecoverabilityContext context)
        {
            var count = message.UserProperties.ContainsKey("RecoveryCount") ?
               (int)message.UserProperties["RecoveryCount"]
               : 0;
            return count;

        }

    

    }
}
