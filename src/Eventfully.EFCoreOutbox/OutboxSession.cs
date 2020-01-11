using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json;
using Eventfully.Outboxing;

namespace Eventfully.EFCoreOutbox
{

    /// <summary>
    /// OutboxSession should have a transient lifetimetime. the DbContext should be injected
    /// </summary>
    /// <typeparam name="T">Implementation of DbContext</typeparam>
    public class OutboxSession<T> : IOutboxSession //, IDisposable
        where T : DbContext
    {
        private static ILogger<OutboxSession<T>> _log = Logging.CreateLogger<OutboxSession<T>>();

        private readonly ConcurrentQueue<OutboxMessage> _transientMessageQueue = new ConcurrentQueue<OutboxMessage>();
        private readonly Outbox<T> _outbox;
        private readonly T _dbContext;

        public OutboxSession(Outbox<T> outbox, T dbContext)
        {
            _outbox = outbox;

            this._dbContext = dbContext
                ?? throw new InvalidOperationException("EFCore Outbox requires a DbContext.  DbContextFactory can not be null");

            if (this._dbContext is ISupportTransientDispatch)
            {
                (this._dbContext as ISupportTransientDispatch).ChangesPersisted += new EventHandler(delegate (Object o, EventArgs a)
                {
                    DispatchTransient();
                });
            }
        }

        /// <summary>
        /// Record the message to the outbox within the current transaction/ Unit of Work
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        public Task Dispatch(string messageTypeIdentifier, byte[] message, MessageMetaData meta, IEndpoint endpoint = null, OutboxDispatchOptions options = null)
        {
            return this._addToContext(messageTypeIdentifier, message, meta, endpoint, options);
        }

        private Task _addToContext(string messageTypeIdentifier, byte[] message, MessageMetaData meta, IEndpoint endpoint = null, OutboxDispatchOptions options = null)
        {

            options = options ?? new OutboxDispatchOptions();
            double delayInSeconds = 0; //send immediately
            bool skipTransient = _outbox.DisableTransientDispatch;

            if (!skipTransient)
                skipTransient = options.SkipTransientDispatch;//check at the message level

            if (options.Delay.HasValue)
                delayInSeconds = options.Delay.Value.TotalSeconds;

            var priorityDateUtc = DateTime.UtcNow.AddSeconds(delayInSeconds);

            var outboxMessage = new OutboxMessage(
                messageTypeIdentifier,
                message,
                meta != null ? JsonConvert.SerializeObject(meta) : null,
                priorityDateUtc,
                endpoint?.Name,
                skipTransient,
                options.ExpiresAtUtc);

            //add the message for persistance with the dbcontext
            _dbContext.Add(outboxMessage);

            if (!outboxMessage.SkipTransientDispatch)
                this._transientMessageQueue.Enqueue(outboxMessage);

            return Task.CompletedTask;
        }

        private void DispatchTransient()
        {
            if (_transientMessageQueue.Count < 1)
                return;

            List<OutboxMessage> tempTransient = new List<OutboxMessage>();
            while (!this._transientMessageQueue.IsEmpty)
            {
                OutboxMessage outboxMessage;
                if (_transientMessageQueue.TryDequeue(out outboxMessage))
                    tempTransient.Add(outboxMessage);
            }
            _outbox.DispatchTransientMessages(tempTransient);

        }




    }
}



