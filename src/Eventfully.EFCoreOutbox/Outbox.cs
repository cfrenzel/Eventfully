using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Dapper;
using Eventfully.Outboxing;

namespace Eventfully.EFCoreOutbox
{
    public interface ISupportTransientDispatch
    {
        event EventHandler ChangesPersisted;
    }

    public enum OutboxMessageStatus
    {
        Ready = 0,
        InProgress = 1,
        Processed = 2,
        Failed = 100,
    }

    public class OutboxSettings
    {
        public string SqlConnectionString { get; set; }
        public bool DisableTransientDispatch { get; set; } = false;

        public int MaxTries { get; set; } = 12;
        public int BatchSize { get; set; } = 50;

        public int MaxConcurrency { get; set; } = 1;
    }


    public class Outbox<T> : IOutbox
    {
        private static ILogger<Outbox<T>> _log = Logging.CreateLogger<Outbox<T>>();

        private readonly BlockingCollection<OutboxMessage> _transientDispatchQueue = new BlockingCollection<OutboxMessage>();
        private readonly SqlConnectionFactory _dbConnection;

        public readonly bool DisableTransientDispatch  = false;
        public readonly int MaxTries  = 12;
        public readonly int BatchSize  = 50;
        public readonly int MaxConcurrency  = 1;

        public Outbox(OutboxSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException("OutboxSettings must not be null");
       
            if (String.IsNullOrEmpty(settings.SqlConnectionString))
                throw new InvalidOperationException("EFCore Outbox requires a connectionString.  ConnectionString can not be null");
            _dbConnection = new SqlConnectionFactory(settings.SqlConnectionString);

            this.MaxTries = settings.MaxTries;
            this.BatchSize = settings.BatchSize;
            this.MaxConcurrency = settings.MaxConcurrency;
            this.DisableTransientDispatch = settings.DisableTransientDispatch;
            
            if (!this.DisableTransientDispatch)
                _beginConsumingTransient(this.MaxConcurrency);
        }

        /// <summary>
        /// Get outbox messages for relay/dispatch
        /// </summary>
        /// <param name="relayCallback"></param>
        /// <returns></returns>
        public async Task<OutboxRelayResult> Relay(Func<string, byte[], MessageMetaData, string, Task> relayCallback)
        {
            try
            {
                using (var conn = _dbConnection.Get())
                {
                    var sql = @"
                    with NextBatch as (
		                select top(@BatchSize) *
		                from OutboxMessages with (rowlock, readpast)
		                where [Status] = 0 and PriorityDateUtc <= @CurrentDateUtc
		                order by PriorityDateUtc
	                )
                    update NextBatch SET [Status] = 1, TryCount = NextBatch.TryCount + 1 
	                OUTPUT inserted.Id, inserted.PriorityDateUtc, inserted.[Type], inserted.Endpoint,
                           inserted.TryCount, inserted.[Status], inserted.ExpiresAtUtc, inserted.CreatedAtUtc,
                           od.Id, od.[Data], od.MetaData
	                FROM NextBatch
	                INNER JOIN OutboxMessageData od
		                ON NextBatch.Id = od.Id
                    ";
                    var outboxMessages = await conn.QueryAsync<OutboxMessage, OutboxMessage.OutboxMessageData, OutboxMessage>(sql, (ov, ovd) =>
                    {
                        ov.MessageData = ovd;
                        return ov;
                    }, new { @BatchSize = this.BatchSize, @CurrentDateUtc = DateTime.UtcNow });

                    Parallel.ForEach(outboxMessages, new ParallelOptions { MaxDegreeOfParallelism = this.MaxConcurrency },
                        async outboxMessage =>
                        {
                            await _relay(outboxMessage, relayCallback);
                        });
                 
                    return new OutboxRelayResult(outboxMessages != null ? outboxMessages.Count() : 0, this.BatchSize);
                }//using
            }
            catch (DbException exDb)
            {
                _log.LogError(exDb, "Database Exception reading outbox");
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Exception reading outbox");
                throw;
            }
        }

        private async Task _relay(OutboxMessage outboxMessage, Func<string, byte[], MessageMetaData, string, Task> relayCallback)
        {
            if (outboxMessage.TryCount > this.MaxTries || outboxMessage.IsExpired(DateTime.UtcNow))
                MarkAsFailure(outboxMessage, permanent: true);
            else
            {
                try
                {
                    var metaData = outboxMessage.MessageData.MetaData != null ? JsonConvert.DeserializeObject<MessageMetaData>(outboxMessage.MessageData.MetaData) : null;
                    await relayCallback(outboxMessage.Type, outboxMessage.MessageData.Data, metaData, outboxMessage.Endpoint);
                    MarkAsComplete(outboxMessage);
                }
                catch (Exception ex)
                {
                    MarkAsFailure(outboxMessage);
                    _log.LogError(ex, "Error publishing Outbox Event Id: {id} Type: {type} Tries: {retryCount}", outboxMessage.Id, outboxMessage.Type, outboxMessage.TryCount);
                }
            }//else
        }

        public async Task CleanUp(TimeSpan cleanupAge)
        {
            using (var conn = _dbConnection.Get())
            {
                try
                {
                    var sql = @"
                    delete from OutboxMessages 
                    where PriorityDateUtc <= @AgeLimitDate and [Status] = 2;
                    ";
                    var res = await conn.ExecuteAsync(sql, new
                    {
                        @AgeLimitDate = DateTime.UtcNow.Add(cleanupAge.Negate()),
                    });
                }
                catch (DbException exDb)
                {
                    _log.LogError(exDb, "Database Exception cleaning up outbox");
                    throw;
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Exception cleaning up outbox");
                    throw;
                }
            }
        }

        public async Task Reset(TimeSpan resetAge)
        {
            using (var conn = _dbConnection.Get())
            {
                try
                {
                    var sql = @"
                    update OutboxMessages Set Status = 0 
                    where PriorityDateUtc <= @ResetLimitDate and [Status] = 1;                    
                    ";
                    var res = await conn.ExecuteAsync(sql, new
                    {
                        @ResetLimitDate = DateTime.UtcNow.Add(resetAge.Negate())
                    });
                }
                catch (DbException exDb)
                {
                    _log.LogError(exDb, "Database Exception resetting outbox");
                    throw;
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Exception resetting outbox");
                    throw;
                }
            }
        }

        private void MarkAsComplete(OutboxMessage outboxMessage)
        {
            try
            {
                using (var conn = _dbConnection.Get())
                {
                    conn.Open();
                    IDbCommand command = conn.CreateCommand();
                    command.CommandText = "update dbo.OutboxMessages SET [Status] = 2  where Id = @Id";
                    SqlParameter parameter = new SqlParameter("@Id", outboxMessage.Id)
                    {
                        SqlDbType = SqlDbType.UniqueIdentifier,
                        Direction = ParameterDirection.Input,
                    };
                    command.Parameters.Add(parameter);
                    int rows = command.ExecuteNonQuery();
                }
            }
            catch (DbException exDb)
            {
                _log.LogError(exDb, "Database Exception marking outbox message complete Message Id: {id} Type: {type} Tries: {retryCount}", outboxMessage.Id, outboxMessage.Type, outboxMessage.TryCount);
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Exception marking outbox message complete Message Id: {id} Type: {type} Tries: {retryCount}", outboxMessage.Id, outboxMessage.Type, outboxMessage.TryCount);
                throw;
            }

        }

        private void MarkAsFailure(OutboxMessage outboxEvent, bool permanent = false)
        {
            try
            {
                using (var conn = _dbConnection.Get())
                {

                    conn.Open();
                    IDbCommand command = conn.CreateCommand();
                    command.CommandText = @"
                        update dbo.OutboxMessages SET [Status] = @Status, PriorityDateUtc = @PriorityDate
                        where Id = @Id and [Status] = 1";

                    SqlParameter idParam = new SqlParameter("@Id", outboxEvent.Id)
                    {
                        SqlDbType = SqlDbType.UniqueIdentifier,
                        Direction = ParameterDirection.Input,
                    };

                    //reset the status to 0 for reprocessing
                    SqlParameter statusParam = new SqlParameter("@Status", permanent ? OutboxMessageStatus.Failed : OutboxMessageStatus.Ready)
                    {
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                    };

                    var nowUtc = DateTime.UtcNow;
                    var counter = outboxEvent.TryCount > 0 ? outboxEvent.TryCount : 1;

                    //2^3,2^4, 2^5... (8,16,32,64,128 (2min), 256 (4min), 512 (8min), 1024 (17min), 2048 (34min), 4096 (1hr) , 8192 ( 2.2hr) ....)
                    var priorityDate = nowUtc.AddSeconds(Math.Pow(2, counter + 2));

                    //update the priority date with a backoff strategy
                    SqlParameter priorityDateParam = new SqlParameter("@PriorityDate", permanent ? outboxEvent.PriorityDateUtc : priorityDate)
                    {
                        SqlDbType = SqlDbType.DateTime2,
                        Direction = ParameterDirection.Input,
                    };
                    command.Parameters.Add(idParam);
                    command.Parameters.Add(statusParam);
                    command.Parameters.Add(priorityDateParam);

                    int rows = command.ExecuteNonQuery();
                }
            }
            catch (DbException exDb)
            {
                _log.LogError(exDb, "Database Exception marking outbox message. failure Message Id: {id} Type: {type} Tries: {retryCount}", outboxEvent.Id, outboxEvent.Type, outboxEvent.TryCount);
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Exception marking outbox message as failure. Message Id: {id} Type: {type} Tries: {retryCount}", outboxEvent.Id, outboxEvent.Type, outboxEvent.TryCount);
                throw;
            }

        }

        /// <summary>
        /// Block on the transient queue in the background
        /// </summary>
        /// <param name="maxConcurrency"></param>
        public void _beginConsumingTransient(int maxConcurrency = 1)
        {
            Task.Run(() =>
            {
                Parallel.ForEach(_transientDispatchQueue.GetConsumingPartitioner(),
                    new ParallelOptions { MaxDegreeOfParallelism = maxConcurrency },
                    async outboxMessage =>
                    {
                        if (outboxMessage.SkipTransientDispatch)
                            return;
                        await _relay(outboxMessage, MessagingService.Instance.DispatchTransientCore);
                    });
            });
        }


        public void DispatchTransientMessages(IEnumerable<OutboxMessage> outboxMessages)
        {
            foreach(var message in outboxMessages)
                _transientDispatchQueue.Add(message);
        }


    }


    /// <summary>
    /// Helper for using BlockingCollection with Parallel.Foreach
    /// https://devblogs.microsoft.com/pfxteam/parallelextensionsextras-tour-4-blockingcollectionextensions/
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection"></param>
    /// <returns></returns>
    public static class Extensions
    {
        public static Partitioner<T> GetConsumingPartitioner<T>(this BlockingCollection<T> collection)
        {
            return new BlockingCollectionPartitioner<T>(collection);
        }

        private class BlockingCollectionPartitioner<T> : Partitioner<T>
        {
            private BlockingCollection<T> _collection;
            public override bool SupportsDynamicPartitions => true;
            internal BlockingCollectionPartitioner(BlockingCollection<T> collection)
            {
                if (collection == null)
                    throw new ArgumentNullException("collection");
                _collection = collection;
            }
           
            public override IList<IEnumerator<T>> GetPartitions(int partitionCount)
            {
                if (partitionCount < 1)
                    throw new ArgumentOutOfRangeException("partitionCount");

                var dynamicPartitioner = GetDynamicPartitions();
                return Enumerable.Range(0, partitionCount).Select(_ => dynamicPartitioner.GetEnumerator()).ToArray();
            }

            public override IEnumerable<T> GetDynamicPartitions()
            {
                return _collection.GetConsumingEnumerable();
            }
        }
    }
}