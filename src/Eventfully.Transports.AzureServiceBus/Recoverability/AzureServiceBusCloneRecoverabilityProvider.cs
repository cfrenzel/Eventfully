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
        private int _maxCompletionRetry = 1;

        public AzureServiceBusCloneRecoverabilityProvider()
        {
            _completeImmediateRetryPolicy = Policy
                .Handle<Exception>()
                .RetryAsync(_maxCompletionRetry);
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
               DateTime.UtcNow.Add(_getRetryInterval(++count))
            );

            //complete the original message - we've already scheduled a clone
            await _completeImmediateRetryPolicy.ExecuteAsync(() =>
                 context.Receiver.CompleteAsync(context.Message.SystemProperties.LockToken)
            );
        }

        private async Task<bool> _handleMaxRetry(int recoveryCount, Message message, RecoverabilityContext context)
        {
            if (message.SystemProperties.DeliveryCount > 1 || recoveryCount > 9)
            {
                await context.Receiver.DeadLetterAsync(message.SystemProperties.LockToken);
                return true;
            }
            return false;
        }

        private int _getRecoveryCount(Message message, RecoverabilityContext context)
        {
            var count = message.UserProperties.ContainsKey("RecoveryCount") ?
               (int)message.UserProperties["RecoveryCount"]
               : 0;
            return count;

        }

        private TimeSpan _getRetryInterval(int retryCount)
        {
            retryCount = retryCount == 0 ? 1 : retryCount;
            //2^3,2^4, 2^5... (8,16,32,64,128 (2min), 256 (4min), 512 (8min), 1024 (17min), 2048 (34min), 4096 (1hr) , 8192 ( 2.2hr) ....)
            var seconds = Math.Pow(2, retryCount + 2);
            return TimeSpan.FromSeconds(seconds);
        }


    }
}
