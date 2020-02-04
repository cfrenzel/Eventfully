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

namespace Eventfully.Semaphore.SqlServer.IntegrationTests
{

    public class IntegrationTestFixture //: IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        
        protected static IConfigurationRoot _config;
        protected static IServiceProvider _serviceProvider;
        protected static readonly Checkpoint _checkpoint;
      
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
           
            _serviceProvider = services.BuildServiceProvider();
       
            _checkpoint = new Checkpoint()
            {
                TablesToIgnore = new[]
                {
                    "__EFMigrationsHistory",
                },
               //SchemasToExclude = new[]{}
            };

           

        }

        public static string ConnectionString => _config.GetConnectionString("ApplicationConnection");

        public static Task ResetCheckpoint() => _checkpoint.Reset(ConnectionString);

        public static IServiceScope NewScope() => _serviceProvider.CreateScope();

       
       

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
