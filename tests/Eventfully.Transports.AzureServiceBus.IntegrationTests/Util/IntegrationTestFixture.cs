using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Respawn;
using FakeItEasy;
using System.Reflection;
using Newtonsoft.Json;
using System.Text;
using Eventfully.Transports.AzureServiceBus;
using Microsoft.Azure.ServiceBus;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Eventfully.Transports.AzureServieBus.IntegrationTests
{
    public class IntegrationTestFixture //: IDesignTimeDbContextFactory<ApplicationDbContext>
    {

        protected static IConfigurationRoot _config;
        protected static IServiceProvider _serviceProvider;
        protected static readonly Checkpoint _checkpoint;

        protected static AzureServiceBusTransportFactory _transportFactory;
        protected static ITransport _transport;

        protected static string _topicEndpointName = "TestAbsTopic";
        protected static IEndpoint _topicEndpoint;

        protected static string _queueEndpointName = "TestAbsQueue";
        protected static IEndpoint _queueEndpoint;

        protected static string _subscriptionEndpointName = "TestAbsSubscription";
        protected static IEndpoint _subscriptionEndpoint;

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

            services.AddLogging(builder =>
            {
                builder.AddConfiguration(_config.GetSection("Logging"));
                //builder.AddConsole();
                builder.AddDebug();
            });


            //services.AddDbContext<ApplicationDbContext>(options =>
            //     options.UseSqlServer(
            //        _config.GetConnectionString("ApplicationConnection")
            //));

            //services.AddMediatR(typeof(Program).GetTypeInfo().Assembly);
            _serviceProvider = services.BuildServiceProvider();
            Logging.LoggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();

            _checkpoint = new Checkpoint()
            {
                TablesToIgnore = new[]
                {
                    "__EFMigrationsHistory",
                },
                //SchemasToExclude = new[]{}
            };

            _transportFactory = new AzureServiceBusTransportFactory();
            _transport = _transportFactory.Create(new AzureServiceBusTransportSettings());
            _queueEndpoint = new Endpoint(
                new EndpointSettings(_queueEndpointName, _config.GetSection("QueueConnectionString").Value)
                {
                    IsReader = true,
                    IsWriter = true,
                }, _transport);

            _topicEndpoint = new Endpoint(
                new EndpointSettings(_topicEndpointName, _config.GetSection("TopicConnectionString").Value)
                {
                    IsWriter = true,
                }, _transport);
            _subscriptionEndpoint = new Endpoint(
                new EndpointSettings(_subscriptionEndpointName, _config.GetSection("SubscriptionConnectionString").Value)
                {
                    IsReader = true,
                }, _transport);
        }

        public static string ConnectionString => _config.GetConnectionString("ApplicationConnection");

        public static Task ResetCheckpoint() => _checkpoint.Reset(ConnectionString);

        public static IServiceScope NewScope() => _serviceProvider.CreateScope();

        public static ITransport Transport => _transport;
        public static IEndpoint TopicEndpoint => _topicEndpoint;
        public static IEndpoint QueueEndpoint => _queueEndpoint;
        public static IEndpoint SubscriptionEndpoint => _subscriptionEndpoint;

        public static byte[] Serialize(IIntegrationMessage message)
        {
            string messageBody = JsonConvert.SerializeObject(message);
            var messageBytes = Encoding.UTF8.GetBytes(messageBody);
            return messageBytes;
        }


        public static QueueClient ReadFromQueue(IEndpoint queueEndpoint, ITestMessageHandler handler)
        {
            //handler = new THAndler(origMessage);
            var queueClient = new QueueClient(new ServiceBusConnectionStringBuilder(queueEndpoint.Settings.ConnectionString), ReceiveMode.ReceiveAndDelete, RetryPolicy.NoRetry);

            var messageHandlerOptions = new MessageHandlerOptions(handler.HandleException)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = true
            };
            queueClient.RegisterMessageHandler(handler.HandleMessage, messageHandlerOptions);
            return queueClient;
        }

        public static Task ClearQueue(IEndpoint queueEndpoint)
        {
            var handler = new MockHAndler();
            var queueClient = new QueueClient(new ServiceBusConnectionStringBuilder(queueEndpoint.Settings.ConnectionString), ReceiveMode.ReceiveAndDelete, RetryPolicy.NoRetry);
            queueClient.PrefetchCount = 1000;
           
            var messageHandlerOptions = new MessageHandlerOptions(handler.HandleException)
            {
                MaxConcurrentCalls = 10,
                AutoComplete = true
            };
            queueClient.RegisterMessageHandler(handler.HandleMessage, messageHandlerOptions);
            
            return Task.Delay(3000)
                .ContinueWith(x=>queueClient.CloseAsync());
        }

        public static async Task WriteToQueue(IEndpoint queueEndpoint, string messageTypeId, byte[] messageBytes, MessageMetaData meta = null)
        {
            var queueClient = new QueueClient(new ServiceBusConnectionStringBuilder(queueEndpoint.Settings.ConnectionString), ReceiveMode.ReceiveAndDelete, RetryPolicy.NoRetry);
            Message m = new Message(messageBytes);
            new AzureServiceBusMetaDataMapper().ApplyMetaData(m, meta ?? new MessageMetaData(), messageTypeId);
            await queueClient.SendAsync(m);
        }

        public static async Task WriteToTopic(IEndpoint topicEndpoint, string messageTypeId, byte[] messageBytes, MessageMetaData meta = null)
        {
            var topicClient = new TopicClient(new ServiceBusConnectionStringBuilder(topicEndpoint.Settings.ConnectionString), RetryPolicy.NoRetry);
            Message m = new Message(messageBytes);
            new AzureServiceBusMetaDataMapper().ApplyMetaData(m, meta ?? new MessageMetaData(), messageTypeId);
            await topicClient.SendAsync(m);
        }


        public static SubscriptionClient ReadFromTopicSubscription(IEndpoint subscriptionEndpoint, ITestMessageHandler handler)
        {
            var builder = new ServiceBusConnectionStringBuilder(subscriptionEndpoint.Settings.ConnectionString);
            var subName = builder.EntityPath.Substring(
                builder.EntityPath.IndexOf("/subscriptions/") + 15);

            var subscriptionClient = new SubscriptionClient(builder, subName, ReceiveMode.ReceiveAndDelete, RetryPolicy.NoRetry);

            var messageHandlerOptions = new MessageHandlerOptions(handler.HandleException)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = true
            };
            subscriptionClient.RegisterMessageHandler(handler.HandleMessage, messageHandlerOptions);
            return subscriptionClient;
        }


        public class MockHAndler : ITestMessageHandler
        {
            public MockHAndler() { }
            public Task HandleException(ExceptionReceivedEventArgs args) => Task.CompletedTask;
            public Task HandleMessage(Message m, CancellationToken token) => Task.CompletedTask;
        }

        //public class THAndler : ITestMessageHandler
        //{
        //    private byte[] _orig;
        //    public THAndler(byte[] orig)
        //    {
        //        _orig = orig;
        //    }
        //    public Task HandleException(ExceptionReceivedEventArgs args)
        //    {

        //        return Task.CompletedTask;
        //    }

        //    public Task HandleMessage(Message m, CancellationToken token)
        //    {
            
        //        if(m.Body.SequenceEqual(_orig))
        //        {
        //            Console.WriteLine("");
        //        }
        //        return Task.CompletedTask;
        //    }
        //}

        //public static void ClearSubscription(IEndpoint subscriptionEndpoint)
        //{

        //}
        //public ApplicationDbContext CreateDbContext(string[] args)
        //    {
        //        var builder = new ConfigurationBuilder()
        //            .SetBasePath(Directory.GetCurrentDirectory())
        //            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        //            .AddUserSecrets<IntegrationTestFixture>();
        //        var config = builder.Build();
        //        var services = new ServiceCollection();

        //        var migrationsAssembly = typeof(IntegrationTestFixture).GetTypeInfo().Assembly.GetName().Name;

        //        services.AddDbContext<ApplicationDbContext>(
        //            x => x.UseSqlServer(config.GetConnectionString("ApplicationConnection"),
        //            b => b.MigrationsAssembly(migrationsAssembly)
        //           ));

        //        var _serviceProvider = services.BuildServiceProvider();
        //        var db = _serviceProvider.GetService<ApplicationDbContext>();
        //        return db;
        //    }


    }
}
