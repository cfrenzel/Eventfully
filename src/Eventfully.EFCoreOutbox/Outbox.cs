﻿using System;
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
using Eventfully.Outboxing;
using System.Threading;

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

        public IRetryIntervalStrategy RetryStrategy { get; set; }
    }


    public class Outbox<T> : IOutbox
    {
        private static ILogger<Outbox<T>> _log = Logging.CreateLogger<Outbox<T>>();

        private readonly BlockingCollection<OutboxMessage> _transientDispatchQueue = new BlockingCollection<OutboxMessage>();
        private readonly SqlConnectionFactory _dbConnection;
        private readonly IRetryIntervalStrategy _retryStrategy;

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

            _retryStrategy = settings.RetryStrategy ?? new DefaultExponentialRetryStrategy();

            this.MaxTries = settings.MaxTries;
            this.BatchSize = settings.BatchSize;
            this.MaxConcurrency = settings.MaxConcurrency;
            this.DisableTransientDispatch = settings.DisableTransientDispatch;
            
        }


        /// <summary>
        /// Get outbox messages for relay
        /// </summary>
        /// <param name="relayCallback"></param>
        /// <returns></returns>
        public async Task<OutboxRelayResult> Relay(Dispatcher dispatcher)//Func<string, byte[], MessageMetaData, string, Task> relayCallback)
        {
            try
            {
                using (var conn = _dbConnection.Get())
                {
                    var sql = @"
                    WITH NextBatch as (
		                select top(@BatchSize) *
		                from OutboxMessages with (rowlock, readpast)
		                where [Status] = 0 and PriorityDateUtc <= @CurrentDateUtc
		                order by PriorityDateUtc
	                )
                    UPDATE NextBatch SET [Status] = 1, TryCount = NextBatch.TryCount + 1 
	                OUTPUT inserted.Id, inserted.PriorityDateUtc, inserted.[Type], inserted.Endpoint,
                           inserted.TryCount, inserted.[Status], inserted.ExpiresAtUtc, inserted.CreatedAtUtc,
                           od.Id, od.[Data], od.MetaData
	                FROM NextBatch
	                INNER JOIN OutboxMessageData od
		                ON NextBatch.Id = od.Id
                    ";

                    conn.Open();
                    DbCommand command = conn.CreateCommand();
                    command.CommandText = sql;
                    SqlParameter batchParam = new SqlParameter("@BatchSize", this.BatchSize)
                    {
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                    };
                    command.Parameters.Add(batchParam);
                    SqlParameter currDateParam = new SqlParameter("@CurrentDateUtc", DateTime.UtcNow)
                    {
                        SqlDbType = SqlDbType.DateTime2,
                        Direction = ParameterDirection.Input,
                    };
                    command.Parameters.Add(currDateParam);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var outboxMessages = HydrateOutboxMessages(reader);
                        Parallel.ForEach(outboxMessages, new ParallelOptions { MaxDegreeOfParallelism = this.MaxConcurrency },
                            async outboxMessage =>
                            {
                                //await _relay(outboxMessage, relayCallback);
                                await _relay(outboxMessage, dispatcher);

                            });
                        return new OutboxRelayResult(outboxMessages != null ? outboxMessages.Count() : 0, this.BatchSize);
                    }
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

        public IEnumerable<OutboxMessage> HydrateOutboxMessages(DbDataReader reader)
        {
            List<OutboxMessage> outboxMessages = new List<OutboxMessage>();
            while (reader.Read())
            {
                var id = reader.GetGuid(0);
                var priorityDateUtc = reader.GetDateTime(1);
                var type = reader.GetString(2);
                string endpoint = null;
                if (!reader.IsDBNull(3))
                    endpoint = reader.GetString(3);
                var tryCount = reader.GetInt32(4);
                var status = reader.GetInt32(5);
                DateTime? expiresAtUtc = null;
                if (!reader.IsDBNull(6))
                    expiresAtUtc = reader.GetDateTime(6);
                var createdAtUtc = reader.GetDateTime(7);
                var dataId = reader.GetGuid(8);
                var data = (byte[])reader.GetValue(9);
                string metaData = null;
                if (!reader.IsDBNull(10))
                    metaData = reader.GetString(10);
                var outboxMessage = new OutboxMessage(type, data, metaData, priorityDateUtc, endpoint, false, expiresAtUtc, id)
                {
                    TryCount = tryCount,
                    Status = status,
                    CreatedAtUtc = createdAtUtc,
                };
                outboxMessages.Add(outboxMessage);
            }
            return outboxMessages;
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

                    conn.Open();
                    DbCommand command = conn.CreateCommand();
                    command.CommandText = sql;
                    SqlParameter parameter = new SqlParameter("@AgeLimitDate", DateTime.UtcNow.Subtract(cleanupAge))
                    {
                        SqlDbType = SqlDbType.DateTime2,
                        Direction = ParameterDirection.Input,
                    };
                    command.Parameters.Add(parameter);
                    int rows = await command.ExecuteNonQueryAsync();
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

                    conn.Open();
                    DbCommand command = conn.CreateCommand();
                    command.CommandText = sql;
                    SqlParameter parameter = new SqlParameter("@ResetLimitDate", DateTime.UtcNow.Subtract(resetAge))
                    {
                        SqlDbType = SqlDbType.DateTime2,
                        Direction = ParameterDirection.Input,
                    };
                    command.Parameters.Add(parameter);
                    int rows = await command.ExecuteNonQueryAsync();
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

        private void MarkAsFailure(OutboxMessage outboxMessage, bool permanent = false)
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

                    var nowUtc = DateTime.UtcNow;
                    var priorityDate = this._retryStrategy.GetNextDateUtc(outboxMessage.TryCount, nowUtc);

                    if (!_isValidForRetry(outboxMessage))
                        permanent = true;

                    SqlParameter idParam = new SqlParameter("@Id", outboxMessage.Id)
                    {
                        SqlDbType = SqlDbType.UniqueIdentifier,
                        Direction = ParameterDirection.Input,
                    };

                    //reset the status to 0 for reprocessing or 100 for perm failure
                    SqlParameter statusParam = new SqlParameter("@Status", permanent ? OutboxMessageStatus.Failed : OutboxMessageStatus.Ready)
                    {
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Input,
                    };

                    //update the priority date with a backoff strategy
                    SqlParameter priorityDateParam = new SqlParameter("@PriorityDate", permanent ? outboxMessage.PriorityDateUtc : priorityDate)
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
                _log.LogError(exDb, "Database Exception marking outbox message. failure Message Id: {id} Type: {type} Tries: {retryCount}", outboxMessage.Id, outboxMessage.Type, outboxMessage.TryCount);
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Exception marking outbox message as failure. Message Id: {id} Type: {type} Tries: {retryCount}", outboxMessage.Id, outboxMessage.Type, outboxMessage.TryCount);
                throw;
            }

        }

        private async Task _relay(OutboxMessage outboxMessage, Dispatcher dispatcher, bool isTransient=false)
        {
            if (!_isValidForRelay(outboxMessage))
                MarkAsFailure(outboxMessage, permanent: true);
            else
            {
                try
                {
                    var metaData = outboxMessage.MessageData.MetaData != null ? JsonConvert.DeserializeObject<MessageMetaData>(outboxMessage.MessageData.MetaData) : null;
                    //await relayCallback(outboxMessage.Type, outboxMessage.MessageData.Data, metaData, outboxMessage.Endpoint);
                    await dispatcher.Invoke(outboxMessage.Type, outboxMessage.MessageData.Data, metaData, outboxMessage.Endpoint, isTransient);
                    MarkAsComplete(outboxMessage);
                }
                catch (Exception ex)
                {
                    MarkAsFailure(outboxMessage);
                    _log.LogError(ex, "Error publishing Outbox Event Id: {id} Type: {type} Tries: {retryCount}", outboxMessage.Id, outboxMessage.Type, outboxMessage.TryCount);
                }
            }//else
        }

        private bool _isValidForRelay(OutboxMessage outboxMessage)
        {
            if (outboxMessage == null)
                throw new ArgumentNullException("Can not validate null outboxMessage for relay");

            if (outboxMessage.TryCount > this.MaxTries || outboxMessage.IsExpired(DateTime.UtcNow))
                return false;
            return true;

        }

        private bool _isValidForRetry(OutboxMessage outboxMessage)
        {
            if (outboxMessage == null)
                throw new ArgumentNullException("Can not validate null outboxMessage for retry");

            //like relay but since the trycount is incremented upon dequeue here we 
            //check if trycount equals max
            if (outboxMessage.TryCount >= this.MaxTries || outboxMessage.IsExpired(DateTime.UtcNow))
                return false;
            return true;

        }

        public void DispatchTransientMessages(IEnumerable<OutboxMessage> outboxMessages)
        {
            foreach(var message in outboxMessages)
                _transientDispatchQueue.Add(message);
        }

        private CancellationTokenSource _outboxTokenSource;
        private Task _outboxTransientTask;

        public Task StartAsync(Dispatcher dispatcher)
        {
            if (_outboxTokenSource != null)
                return Task.CompletedTask;

            _outboxTokenSource = new CancellationTokenSource();
            CancellationToken ct = _outboxTokenSource.Token;

            if (!this.DisableTransientDispatch)
            {
                _outboxTransientTask = Task.Run(() =>
                {
                   Parallel.ForEach(_transientDispatchQueue.GetConsumingPartitioner(),
                       new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = this.MaxConcurrency },
                       async outboxMessage =>
                       {
                           if (outboxMessage.SkipTransientDispatch)
                               return;
                           await _relay(outboxMessage, dispatcher, true);// MessagingService.Instance.DispatchTransientCore);
                       });
                }, ct);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            if (_outboxTokenSource != null && !_outboxTokenSource.IsCancellationRequested)
                _outboxTokenSource.Cancel();

            return _outboxTransientTask;
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