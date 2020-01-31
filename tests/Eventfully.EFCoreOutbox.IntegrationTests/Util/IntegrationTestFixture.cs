using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Respawn;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Design;
using Eventfully.Handlers;
using Newtonsoft.Json;
using System.Text;

namespace Eventfully.EFCoreOutbox.IntegrationTests
{

    public class IntegrationTestFixture : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        
        protected static IConfigurationRoot _config;
        protected static IServiceProvider _serviceProvider;
        protected static readonly Checkpoint _checkpoint;
        public static TestMessage Message;
        public static byte[] MessageBytes;

        static IntegrationTestFixture()
        {
            _config = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json", optional: true)
              .AddUserSecrets<IntegrationTestFixture>()
              .AddEnvironmentVariables()
              .Build();

            var services = new ServiceCollection();

            services.AddSingleton(_config);

            services.AddLogging(builder => {
                builder.AddConfiguration(_config.GetSection("Logging"));
                //builder.AddConsole();
                builder.AddDebug();
            });

            services.AddDbContext<ApplicationDbContext>(options =>
                 options.UseSqlServer(
                    _config.GetConnectionString("ApplicationConnection")
            ));

            //services.AddMediatR(typeof(Program).GetTypeInfo().Assembly);
            _serviceProvider = services.BuildServiceProvider();
       
            _checkpoint = new Checkpoint()
            {
                TablesToIgnore = new[]
                {
                    "__EFMigrationsHistory",
                },
               //SchemasToExclude = new[]{}
            };

            ///Setup internal logging for eventfully
            Logging.LoggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            
            //var outbox = new Outbox<ApplicationDbContext>(new OutboxSettings()
            //{
            //    BatchSize = 10,
            //    MaxConcurrency = 1,
            //    MaxTries = 10,
            //    SqlConnectionString = ConnectionString,
            //    DisableTransientDispatch = true,
            //});

            //var handlerFactory = A.Fake<IServiceFactory>();
            //var messagingService = new MessagingService(outbox, handlerFactory);

            Message = new TestMessage()
            {
                Id = Guid.NewGuid(),
                Description = "Test Message Text",
                Name = "Test Message Name",
                MessageDate = DateTime.UtcNow,
            };

            string messageBody = JsonConvert.SerializeObject(Message);
            MessageBytes = Encoding.UTF8.GetBytes(messageBody);

        }

        public static string ConnectionString => _config.GetConnectionString("ApplicationConnection");

        public static Task ResetCheckpoint() => _checkpoint.Reset(ConnectionString);

        public static IServiceScope NewScope() => _serviceProvider.CreateScope();

        public static async Task<OutboxMessage> CreateOutboxMessage(string endpointName, string uniqueId, OutboxMessageStatus status, DateTime? priorityDateUtc = null, DateTime? expiresAtUtc = null)
        {
            var messageMetaData = new MessageMetaData(messageId: uniqueId);
            var serializedMessageMetaData = messageMetaData != null ? JsonConvert.SerializeObject(messageMetaData) : null;

            OutboxMessage om = new OutboxMessage(
                Message.MessageType,
                MessageBytes,
                serializedMessageMetaData,
                priorityDateUtc.HasValue ? priorityDateUtc.Value : DateTime.UtcNow,
                endpointName,
                true,
                expiresAtUtc
            )
            {
                Status = (int)status,
            };
            return await CreateOutboxMessage(om);

        }
        public static async Task<OutboxMessage> CreateOutboxMessage(OutboxMessage om)
        {
            using (var scope = NewScope())
            {
           
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                db.Set<OutboxMessage>().Add(om);
                await db.SaveChangesAsync();
                return om;
            }
        }

        public ApplicationDbContext CreateDbContext(string[] args)
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddUserSecrets<IntegrationTestFixture>();
                var config = builder.Build();
                var services = new ServiceCollection();

                var migrationsAssembly = typeof(IntegrationTestFixture).GetTypeInfo().Assembly.GetName().Name;

                services.AddDbContext<ApplicationDbContext>(
                    x => x.UseSqlServer(config.GetConnectionString("ApplicationConnection"),
                    b => b.MigrationsAssembly(migrationsAssembly)
                   ));

                var _serviceProvider = services.BuildServiceProvider();
                var db = _serviceProvider.GetService<ApplicationDbContext>();
                return db;
            }
        

    }
}
