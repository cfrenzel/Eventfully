using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SimulationWorker
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host =  CreateHostBuilder(args).Build();
            await host.RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // services.AddDbContext<ApplicationDbContext>(options =>
                    //     options.UseSqlServer(
                    //         hostContext.Configuration.GetConnectionString("ApplicationConnection"),
                    //         b => b.MigrationsAssembly(typeof(Program).Assembly.FullName)
                    //     )
                    // );
                    services.AddHttpClient();
                    services.AddMassTransit(x =>
                    {
                        
                        //setup a transactional outbox in our sql server
                        // x.AddEntityFrameworkOutbox<ApplicationDbContext>(o =>
                        // {
                        //     o.UseSqlServer();
                        //     o.UseBusOutbox();
                        // });
                        x.SetKebabCaseEndpointNameFormatter();
                        // By default, sagas are in-memory, but should be changed to a durable
                        // saga repository.
                        //x.SetInMemorySagaRepositoryProvider();

                        var entryAssembly = Assembly.GetEntryAssembly();

                        x.AddConsumers(entryAssembly);
                        x.AddSagaStateMachines(entryAssembly);
                        x.AddSagas(entryAssembly);
                        x.AddActivities(entryAssembly);
                        
                        // x.UsingInMemory((context, cfg) =>
                        // {
                        //     cfg.ConfigureEndpoints(context);
                        // });
                        x.UsingAzureServiceBus((context,cfg) =>
                        {
                            cfg.Host("Endpoint=sb://platform-experiment.servicebus.windows.net/;SharedAccessKeyName=platform-experiment;SharedAccessKey=ARSa58de7yen1D5PeFkWwzB7srsPo7+PSD3H1vpFuZE=");
                            cfg.ConfigureEndpoints(context);
                        });

                    });
                    services.AddHostedService<SimulationBackgroundService>();
                })
                ;
    }
}
