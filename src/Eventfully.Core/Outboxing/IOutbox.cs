using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace Eventfully.Outboxing
{
   
    public interface IOutbox
    {
        /// <summary>
        /// Find messages from the outbox that are ready and relay the to their true endpoint
        /// </summary>
        /// <param name="relayCallback">method provided to relay the messages</param>
        /// <returns></returns>
        Task<OutboxDispatchResult> Relay(Func<string, byte[], MessageMetaData, string, Task> relayCallback);

        /// <summary>
        /// Do any periodic cleanup necessary on the inbox 
        /// ex. deleting old processed messages
        /// </summary>
        /// <param name="cleanupAge">the age of old processed messages that can be deleted</param>
        /// <returns></returns>
        Task CleanUp(TimeSpan cleanupAge);

        /// <summary>
        /// Do any periodic resets necessary on the inbox 
        /// ex. resetting messages that began processing but never finished
        /// </summary>
        /// <param name="resetAge">the age of old pending messages that can be reset</param>
        /// <returns></returns>
        Task Reset(TimeSpan resetAge);

    }
}