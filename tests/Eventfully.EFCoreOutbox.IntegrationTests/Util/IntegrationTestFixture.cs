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

namespace Eventfully.EFCoreOutbox.IntegrationTests
{

    public class IntegrationTestFixture : IDesignTimeDbContextFactory<ApplicationDbContext>
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
                //builder.AddDebug();
            });

            services.AddDbContext<ApplicationDbContext>(options =>
                 options.UseSqlServer(
                    _config.GetConnectionString("ApplicationConnection")
            ));

            //services.AddMediatR(typeof(Program).GetTypeInfo().Assembly);

            _serviceProvider = services.BuildServiceProvider();
            //_log = _serviceProvider.GetService<ILogger<Program>>();

            _checkpoint = new Checkpoint()
            {
                TablesToIgnore = new[]
                {
                    "__EFMigrationsHistory",
                },
               //SchemasToExclude = new[]
               //{
               //}
            };

        }

        public static string ConnectionString => _config.GetConnectionString("ApplicationConnection");

        public static Task ResetCheckpoint() => _checkpoint.Reset(ConnectionString);

        public static IServiceScope NewScope() => _serviceProvider.CreateScope();

                //public static async Task ExecuteScopeAsync(Func<IServiceProvider, Task> action)
                //{
                //    using (var scope = _serviceProvider.CreateScope())
                //    {
                //        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();

                //        try
                //        {
                //            await dbContext.BeginTransactionAsync().ConfigureAwait(false);

                //            await action(scope.ServiceProvider).ConfigureAwait(false);

                //            await dbContext.CommitTransactionAsync().ConfigureAwait(false);
                //        }
                //        catch (Exception)
                //        {
                //            dbContext.RollbackTransaction();
                //            throw;
                //        }
                //    }
                //}

                //public static async Task<T> ExecuteScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
                //{
                //    using (var scope = _serviceProvider.CreateScope())
                //    {
                //        var db = scope.ServiceProvider.GetService<ApplicationDbContext>();

                //        try
                //        {
                //            //await dbContext.BeginTransactionAsync().ConfigureAwait(false);

                //           var result = await action(scope.ServiceProvider).ConfigureAwait(false);

                //            await db.SaveChangesAsync();
                //            //await dbContext.CommitTransactionAsync().ConfigureAwait(false);

                //            return result;
                //        }
                //        catch (Exception)
                //        {

                //            //db.RollbackTransaction();
                //            throw;
                //        }
                //    }
                //}



                //private static int CourseNumber = 1;

                //public static int NextCourseNumber() => Interlocked.Increment(ref CourseNumber);


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
