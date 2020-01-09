using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Shouldly;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Eventfully.Handlers;
using FakeItEasy;
using Eventfully.Transports.Testing;
using System.Data.SqlClient;
using Xunit.Abstractions;

namespace Eventfully.EFCoreOutbox.IntegrationTests
{
    using static IntegrationTestFixture;
    
    [Collection("Sequential")]
    public class OutboxConcurrencyTests : IntegrationTestBase, IClassFixture<OutboxConcurrencyTests.Fixture>
    {
        private readonly Fixture _fixture;
        private readonly ITestOutputHelper _log;

        public OutboxConcurrencyTests(Fixture fixture, ITestOutputHelper log)
        {
            this._fixture = fixture;
            this._log = log;
            //clear outbox
            using (var scope = NewScope())
            {
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                db.Set<OutboxMessage>().RemoveRange(db.Set<OutboxMessage>());
                db.SaveChanges();
            }
        }

        public class Fixture
        {
            public Fixture()
            {
            
            }
            public async Task CreateOutboxMessages(int count)
            {
                using (var scope = NewScope())
                {
                    var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                    for (int i = 0; i < count; i++)
                    {
                        var messageMetaData = new MessageMetaData(messageId: i.ToString());
                        var serializedMessageMetaData = messageMetaData != null ? JsonConvert.SerializeObject(messageMetaData) : null;

                        OutboxMessage om = new OutboxMessage(
                            Message.MessageType,
                            MessageBytes,
                            serializedMessageMetaData,
                            DateTime.UtcNow,
                            "Test",
                            true,
                            null
                        );

                        db.Set<OutboxMessage>().Add(om);
                    }
                    await db.SaveChangesAsync();
                }
            }

        }

        [Fact]
        public async Task Should_return_unique_messages_to_concurrent_requests()
        {
            int numMessages = 40;
            int batchSize = 5;
            int numClients = 2;
            int numResultSets = numMessages / batchSize / numClients;

            await _fixture.CreateOutboxMessages(numMessages);
            var sql = $@"
                DECLARE @runAt0 AS TIME = '{DateTime.Now.AddSeconds(3).ToString("HH:mm:ss")}'--'13:32:00' --local time
                DECLARE @nextRun AS NVARCHAR(8) = CONVERT(nvarchar(8), @runAt0, 108);
                DECLARE @BatchSize AS INT = {batchSize} --5
                DECLARE @Counter AS INT =  0
                DECLARE @CurrentDateUtc DateTime
                PRINT @nextRun;

                WAITFOR TIME @nextRun

                WHILE @Counter < {numResultSets}
                BEGIN
                    SET @CurrentDateUtc = GETUTCDATE()
                    SET @Counter = @Counter + 1
	                SET @runAt0 = DATEADD(SECOND, 2, @runAt0);

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
	                set @nextRun = CONVERT(nvarchar(8), @runAt0, 108);
	                WAITFOR TIME @nextRun
                END
            ";


            _log.WriteLine($"sets:{numResultSets}");
            _log.WriteLine($"{sql}");

            List<Guid> results = new List<Guid>();
            
            Parallel.Invoke(
                 () => results.AddRange(RunQuery("query1", sql, numResultSets)),
                 () => results.AddRange(RunQuery("query2", sql, numResultSets))
              );

            results.Count.ShouldBe(numMessages);
            results.Distinct().Count().ShouldBe(numMessages);

        }
        private List<Guid> RunQuery(string label, string sql, int numberResultSets)
        {
            SqlConnection conn;
            SqlCommand cmd;
            List<Guid> results = new List<Guid>();

            using (conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                cmd = new SqlCommand(sql, conn) {/* CommandTimeout = 60*/ };
                using (SqlDataReader sqlReader = cmd.ExecuteReader())
                {
                    for (int i = 0; i < numberResultSets; i++)
                    {
                        _log.WriteLine($"in result set {label}/{i}");
                        while (sqlReader.Read())
                        {
                            var id = sqlReader.GetGuid(0);
                            results.Add(id);
                            _log.WriteLine($"{label}/{i}: {id}");                       
                        }
                        Task.Delay(3000).Wait();
                        sqlReader.NextResult();
                    }
                 }
            }
            return results;
        }
           
        


    }
}
