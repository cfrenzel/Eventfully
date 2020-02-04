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


    public class AzureServiceBusDeferRecoverabilityProvider : IAzureServiceBusRecoverabilityProvider
    {
        private static readonly string _controlMessageLabel = "ControlMessage.Recover";
        private readonly AsyncRetryPolicy _completeImmediateRetryPolicy;
        private readonly AsyncRetryPolicy _receiveDeferredImmediateRetryPolicy;
        private int _maxCompletionImmediateRetry = 1;
        private int _maxReceiveRetry = 1;
        private IRetryIntervalStrategy _retryStrategy;


        public AzureServiceBusDeferRecoverabilityProvider(IRetryIntervalStrategy retryStrategy = null)
        {
            _completeImmediateRetryPolicy = Policy
                .Handle<Exception>()
                .RetryAsync(_maxCompletionImmediateRetry);

            _receiveDeferredImmediateRetryPolicy = Policy
             .Handle<Exception>()
             .RetryAsync(_maxReceiveRetry);
            _retryStrategy = retryStrategy != null ? retryStrategy : new DefaultExponentialRetryStrategy();
        }


        public async Task<RecoverabilityContext> OnPreHandle(RecoverabilityContext context)
        {
            if (IsControlMessage(context.Message))//never try to reschedule the control messages themselves
                return await _handleControlMessage(context.Message, context);
            return context;

        }


        private async Task<RecoverabilityContext> _handleControlMessage(Message serviceBusControlMessage, RecoverabilityContext context)
        {
            try
            {
                var json = Encoding.UTF8.GetString(serviceBusControlMessage.Body);
                var controlMessage = (RecoverabilityControlMessage)JsonConvert.DeserializeObject<RecoverabilityControlMessage>(json);

                context.Message = await _receiveDeferredImmediateRetryPolicy.ExecuteAsync(() =>
                    context.Receiver.ReceiveDeferredMessageAsync(controlMessage.SequenceNumber)
                );
                context.TempData["ControlMessage"] = serviceBusControlMessage;
                context.TempData["ControlMessageContent"] = controlMessage;
                return context;
            }
            catch (Exception exc)
            {
                //wrap all exceptions in a nontransient exception because retry won't help us now 
                ///TODO: if deferred message times-out then it will throw exceptions until the control message retries expire
                ///need to use poly to retry and circuit break 
                throw new NonTransientException("Error handling Recoverability control message", exc);
            }
        }
        public async Task Recover(RecoverabilityContext context)// int timesQueued, Endpoint endpoint, IMessageReceiver receiver, Message controlMessage = null, string description = null, Exception exc = null)
        {
            if (IsControlMessage(context.Message))//never try to reschedule the control messages themselves
                return;

            var count = context.TempData.ContainsKey("ControlMessageContent") ?
                ((RecoverabilityControlMessage)context.TempData["ControlMessageContent"]).RecoveryCount
                : 0;
            var sender = AzureServiceBusClientCache.GetSender(context.Endpoint.Settings.ConnectionString);
            var retryMessage = new RecoverabilityControlMessage(context.Message.SystemProperties.SequenceNumber, ++count);

            //schedule a special control message to be delivered in the future to tell the the deferred message to be retrieved and re processed
            await sender.ScheduleMessageAsync(
                _createServiceBusMessage(retryMessage),
                this._retryStrategy.GetNextDateUtc(retryMessage.RecoveryCount)
            );

            //defer the current message / first time through
            if (!context.TempData.ContainsKey("ControlMessage"))
            {
                await context.Receiver.DeferAsync(context.Message.SystemProperties.LockToken);
            }
            else //already deferred. complete the control message / we just sent a new one above
            {
                await _completeImmediateRetryPolicy.ExecuteAsync(() =>
                     context.Receiver.CompleteAsync(((Message)context.TempData["ControlMessage"]).SystemProperties.LockToken)
                );

                //release the lock on the current deferred message
                await _completeImmediateRetryPolicy.ExecuteAsync(() =>
                    context.Receiver.AbandonAsync(context.Message.SystemProperties.LockToken)
               );
            }
        }

        public async Task OnPostHandle(RecoverabilityContext context)// int timesQueued, Endpoint endpoint, IMessageReceiver receiver, Message controlMessage = null, string description = null, Exception exc = null)
        {
            try
            {
                if (context.TempData.ContainsKey("ControlMessage"))
                {
                    await _completeImmediateRetryPolicy.ExecuteAsync(() =>
                        context.Receiver.CompleteAsync(((Message)context.TempData["ControlMessage"]).SystemProperties.LockToken)
                    );
                }
            }
            catch (Exception exc)
            {
                ///TODO log that control message couldn't be completed
            }
        }

        public bool IsControlMessage(Message m)
        {
            if (m != null && m.Label != null)
                return m.Label.Equals(_controlMessageLabel, StringComparison.OrdinalIgnoreCase);
            return false;

        }

        private Message _createServiceBusMessage(RecoverabilityControlMessage controlMessage)
        {
            return new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(controlMessage)))
            {
                Label = _controlMessageLabel,
            };

        }


        //public async Task Cancel(RecoverabilityContext context)
        //{
        //    if(context.ControlMessage != null)
        //        await context.Receiver.CompleteAsync(context.ControlMessage.SystemProperties.LockToken);

        //}

    }
}
