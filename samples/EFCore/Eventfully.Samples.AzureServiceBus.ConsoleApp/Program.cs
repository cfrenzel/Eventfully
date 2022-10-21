using System;
using System.IO;
using System.Dynamic;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Eventfully;
using Eventfully.Core.Analyzers.Test;
using Eventfully.EFCoreOutbox;
using Eventfully.Semaphore.SqlServer;
using Eventfully.Transports;
using Eventfully.Transports.AzureServiceBus2;

namespace Eventfully.Samples.ConsoleApp
{
    public class Program : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public static IConfigurationRoot _config;
        public static IServiceProvider _serviceProvider;

        public static async Task Main(string[] args)
        {
            await _init();
            
            // create the sender
            var client = new ServiceBusClient(_config.GetConnectionString("AzureServiceBus"), new ServiceBusClientOptions(){ });
            ServiceBusSender sender = client.CreateSender("contracts.events/ordersubmitted");
            ServiceBusMessage message = new ServiceBusMessage("Hello world!");
            await sender.SendMessageAsync(message);
            // await PublishOrderCreatedEvent();
            // await PublishOrderCreatedWithinTransactionWithOrderEntity();
            // await PublishOrderCreatedWithDelay();
            // await PublishOrderCreatedFromRawJson();
            // await PublishEncryptedPaymentMethodCreated();
            // await PublishEventWithFailingHandler();
            //
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }

        static async Task _init()
        {
            var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("appsettings.json", optional: true)
                 .AddUserSecrets<Program>()
                 .AddEnvironmentVariables()
                ;
            _config = builder.Build();

            var _services = new ServiceCollection();
            _services.AddLogging(builder =>
            {
                builder.AddConfiguration(_config.GetSection("Logging"));
                builder.AddDebug();
                //config.AddConsole();
            });

            _services.AddDbContext<ApplicationDbContext>(options =>
                   options.UseSqlServer(
                       _config.GetConnectionString("ApplicationConnection")
            ));
            
            _services.AddMessaging(options =>
            {
                options.UseEFCore<ApplicationDbContext>(_config.GetConnectionString("ApplicationConnection"), cfg => { } );
                options.UseAzureServiceBus(_config.GetConnectionString("AzureServiceBus"),
                    cfg =>
                    {
                        ///TODO: specify a library/namespace/filter to look for messages, could implement interfaces or have a certain naming convention
                        ///TODO: allow not passing in name and allow convention to name queue/topic 
                        cfg.ConfigureTopic<PizzaOrderedEvent>("contracts.events/ordersubmitted", "test-sub");
                    });
            });
                /*  settings =>
                {
                    settings.OutboxConsumerSemaphore = new SqlServerSemaphore(_config.GetConnectionString("ApplicationConnection"), "dev.outbox.consumer", 30, 3);        
                },
                typeof(Program).GetTypeInfo().Assembly*/
            
            _serviceProvider = _services.BuildServiceProvider();

            await _serviceProvider.GetService<ServiceBusManager>().StartAsync(CancellationToken.None);
        }

        static async Task PublishOrderCreatedEvent()
        {
            var client = _serviceProvider.GetService<IMessagingClient>();
            var db = _serviceProvider.GetService<ApplicationDbContext>();

            Console.WriteLine("Publishing OrderCreated Event");
            
            await client.Publish(new OrderCreated(Guid.NewGuid(),522.99M,"USD",
                new OrderCreated.Address("123 Buyer St",null,"Nashville","TN","37067")
            ));
            await db.SaveChangesAsync();
        }

        static async Task PublishOrderCreatedWithinTransactionWithOrderEntity()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var client = scope.ServiceProvider.GetService<IMessagingClient>();
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
                Order o = new Order()
                {
                     Id = MassTransit.NewId.NextGuid(),
                     Number = "1111",
                     CurrencyCode = "USD",
                     Total = 522.99M,
                };
                db.Orders.Add(o);
                Console.WriteLine("Publishing OrderCreated Event With Order Entity");
                await client.Publish(new OrderCreated(o.Id, o.Total, o.CurrencyCode,
                    new OrderCreated.Address("615 Transact Dr", null, "Atlanta", "GA", "30319")
                ));
                await db.SaveChangesAsync();
            }
        }


        static async Task PublishOrderCreatedWithDelay()
        {
            var client = _serviceProvider.GetService<IMessagingClient>();
            var db = _serviceProvider.GetService<ApplicationDbContext>();

            Console.WriteLine("Publishing OrderCreated Event with 20 Second Delay");

            await client.Publish(new OrderCreated(Guid.NewGuid(), 722.99M, "USD",
                new OrderCreated.Address("123 Delayed St", null, "Nashville", "TN", "37067")),
                new MessageMetaData(delay:TimeSpan.FromSeconds(20))
            );
            await db.SaveChangesAsync();
        }

        static async Task PublishOrderCreatedFromRawJson()
        {
            var client = _serviceProvider.GetService<IMessagingClient>();
            var db = _serviceProvider.GetService<ApplicationDbContext>();

            Console.WriteLine("Publishing OrderCreated Event From Json");

            dynamic json = new ExpandoObject();
            json.OrderId = Guid.NewGuid();
            json.TotalDue = 622.99M;
            json.CurrencyCode = "USD";
            json.ShippingAddress = new
            {
                Line1 = "456 Peachtree St",
                Line2 = "Suite A",
                City = "Atlanta",
                StateCode = "GA",
                Zip = "30319"
            };

            await client.Publish(
                "Sales.OrderCreated",
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(json))
            );
            await db.SaveChangesAsync();
        }


        static async Task PublishEncryptedPaymentMethodCreated()
        {
            var client = _serviceProvider.GetService<IMessagingClient>();
            var db = _serviceProvider.GetService<ApplicationDbContext>();

            Console.WriteLine("Publishing PaymentMethodCreated Event");

            await client.Publish(new PaymentMethodCreated(Guid.NewGuid(),
                new PaymentMethodCreated.CardInfo("1111-1111-1111-1111", "John Doe", "2030", "11")
            ));
            await db.SaveChangesAsync();
        }


        static async Task PublishEventWithFailingHandler()
        {
            var client = _serviceProvider.GetService<IMessagingClient>();
            var db = _serviceProvider.GetService<ApplicationDbContext>();

            var ev = new FailingHandler.Event(DateTime.UtcNow.AddMinutes(4));
            Console.WriteLine($"Publishing Event with failing handler - Id: {ev.Id}");
            await client.Publish(ev);
            await db.SaveChangesAsync();
        }

        public ApplicationDbContext CreateDbContext(string[] args)
            {
                _init();
                return _serviceProvider.GetService<ApplicationDbContext>();
            }
        
    }
}
