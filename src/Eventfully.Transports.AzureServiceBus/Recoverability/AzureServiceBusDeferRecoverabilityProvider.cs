﻿using Microsoft.Azure.ServiceBus;
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
        private int _maxCompletionRetry = 1;
        private int _maxReceiveRetry = 1;

        public AzureServiceBusDeferRecoverabilityProvider() 
        {
            _completeImmediateRetryPolicy = Policy
                .Handle<Exception>()
                .RetryAsync(_maxCompletionRetry);

            _receiveDeferredImmediateRetryPolicy = Policy
             .Handle<Exception>()
             .RetryAsync(_maxReceiveRetry);
        }


        public  async Task<RecoverabilityContext> OnPreHandle(RecoverabilityContext context)
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
                //wrap all exceptions in a nontransient exception because retyr won't help us now 
                ///TODO: if deferred message times out then it will throw exceptions until the control message retries expire
                ///need to use poly to retry and circuit break 
                throw new NonTransientException("Error handling Recoverability control message", exc);
            }
        }

        //public async Task Recover(Message message, int timesQueued,  Endpoint endpoint, IMessageReceiver receiver, Message controlMessage = null, string description = null,  Exception exc = null)
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
                DateTime.UtcNow.Add(_getRetryInterval(retryMessage.RecoveryCount))
            );
            //defer the current message the first time through
            if (!context.TempData.ContainsKey("ControlMessage"))
            {
                await context.Receiver.DeferAsync(context.Message.SystemProperties.LockToken);
            }
            else //already deferred. complete the previous control message / we already sent a new one above
            {
                await _completeImmediateRetryPolicy.ExecuteAsync(() =>
                     context.Receiver.CompleteAsync(((Message)context.TempData["ControlMessage"]).SystemProperties.LockToken)
                );
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
        
        private TimeSpan _getRetryInterval(int retryCount)
        {
            retryCount = retryCount == 0 ? 1 : retryCount;
            //2^3,2^4, 2^5... (8,16,32,64,128 (2min), 256 (4min), 512 (8min), 1024 (17min), 2048 (34min), 4096 (1hr) , 8192 ( 2.2hr) ....)
            var seconds = Math.Pow(2, retryCount + 2);
            return TimeSpan.FromSeconds(seconds);
        }

        //public async Task Cancel(RecoverabilityContext context)
        //{
        //    if(context.ControlMessage != null)
        //        await context.Receiver.CompleteAsync(context.ControlMessage.SystemProperties.LockToken);

        //}

    }
}
