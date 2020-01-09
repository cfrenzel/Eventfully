using System;
using System.IO;
using System.Dynamic;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Eventfully.EFCoreOutbox;

namespace Eventfully.Samples.ConsoleApp
{
    public class Program : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public static IConfigurationRoot _config;
        public static IServiceProvider _serviceProvider;

        public static async Task Main(string[] args)
        {
            _init();
            
            await PublishOrderCreated();
            await PublishOrderCreatedWithDelay();
            await PublishOrderCreatedFromRawJson();
            await PublishEncryptedPaymentMethodCreated();

            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }

        static void _init()
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
            }
            );

            _services.AddDbContext<ApplicationDbContext>(options =>
                   options.UseSqlServer(
                       _config.GetConnectionString("ApplicationConnection")
            ));

            _services.AddEFCoreOutbox<ApplicationDbContext>(settings =>
            {
                settings.DisableTransientDispatch = false;
                settings.MaxConcurrency = 1;
                settings.SqlConnectionString = _config.GetConnectionString("ApplicationConnection");
            });

            _services.AddMessaging(
                new MessagingProfile(_config),
                null,
                typeof(Program).GetTypeInfo().Assembly
            );

            _serviceProvider = _services.BuildServiceProvider();

            //enable receiving messages from the configured endpoints
            _serviceProvider.UseMessagingHost();
        }

        static async Task PublishOrderCreated()
        {
            var client = _serviceProvider.GetService<IMessagingClient>();
            var db = _serviceProvider.GetService<ApplicationDbContext>();

            Console.WriteLine("Publishing OrderCreated Event");
            
            await client.Publish(new OrderCreated(Guid.NewGuid(),522.99M,"USD",
                new OrderCreated.Address("123 Buyer St",null,"Nashville","TN","37067")
            ));
            await db.SaveChangesAsync();
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


      
            public ApplicationDbContext CreateDbContext(string[] args)
            {
                _init();
                return _serviceProvider.GetService<ApplicationDbContext>();
            }
        
    }
}
