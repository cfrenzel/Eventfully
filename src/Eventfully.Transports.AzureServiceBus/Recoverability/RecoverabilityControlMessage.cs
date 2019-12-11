using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.Transports.AzureServiceBus
{
    public class RecoverabilityControlMessage 
    {
        public int RecoveryCount { get; set; } = 1;
        public Int64 SequenceNumber { get; set; }

        public RecoverabilityControlMessage()
        {

        }

        public RecoverabilityControlMessage(Int64 sequenceNumber, int recoveryScheduledCount = 1)
        {
            recoveryScheduledCount = recoveryScheduledCount < 1 ? 1 : recoveryScheduledCount;
            this.SequenceNumber = sequenceNumber;
            this.RecoveryCount = recoveryScheduledCount;
        }


        //public static RecoverabilityControlMessage FromServiceBusMessage(Message)
        //public static Message Create(Int64 sequenceNumber, int lastRecoveryCount = 0)
        //{
        //    lastRecoveryCount = lastRecoveryCount < 0 ? 0 : lastRecoveryCount;
        //    var recoverMessage = new RecoverabilityControlMessage(sequenceNumber, lastRecoveryCount++);
        //    return new Message(
        //           Encoding.UTF8.GetBytes(
        //               JsonConvert.SerializeObject(recoverMessage)
        //           )
        //    )
        //}

    }
}
