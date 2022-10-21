using System.Net.Mime;
using System.Reflection;
using Contracts.Commands;
using Contracts.Events;
using Eventfully;
using Microsoft.EntityFrameworkCore;
using OrderingApi;
using OrderingApi.ApplicationServices;
using OrderingApi.Entities;
using Sample.Api.Processes;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<OrderService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ApplicationConnection"),
        b => b.MigrationsAssembly(typeof(Program).Assembly.FullName)
    )
);

builder.Services.AddMessaging(options =>
    {
        options.UseEfCore<ApplicationDbContext>(builder.Configuration.GetConnectionString("ApplicationConnection"), cfg =>
        {
        });
        
        options.UseAzureServiceBus(builder.Configuration.GetConnectionString("AzureServiceBus"), cfg =>
        {
            cfg.ConfigureEndpoint("payment-commands")
                .BindCommand<ProcessPayment>()
                .BindCommand<CancelPayment>()
                //.UseAesEncryption(builder.Configuration.GetSection("SampleAESKey").Value)
                ;

            cfg.ConfigureEndpoint("order-events")
                .BindEvent<PaymentRequestTimeout>()
                .BindEvent<OrderCreated>()
                .UseAesEncryption(builder.Configuration.GetSection("SampleAESKey").Value)
                ;
        
            cfg.RegisterHandlers(typeof(Program).GetTypeInfo().Assembly);
            cfg.CreateEndpoints();
        });
    }
);



/*
builder.Services.AddMassTransit(x =>
{
    //setup a transactional outbox in our sql server
    x.AddEntityFrameworkOutbox<ApplicationDbContext>(o =>
    {
        o.UseSqlServer();
        o.UseBusOutbox();
    });
    x.AddSagaStateMachine<OrderSubmissionProcess, OrderState>().EntityFrameworkRepository(r =>
    {
        r.ExistingDbContext<ApplicationDbContext>(); 
        //r.LockStatementProvider = new PostgresLockStatementProvider();
    });
    
    x.SetKebabCaseEndpointNameFormatter();
    var entryAssembly = Assembly.GetEntryAssembly();
    x.AddConsumers(entryAssembly);
    x.AddSagaStateMachines(entryAssembly);
    x.AddSagas(entryAssembly);
    x.AddActivities(entryAssembly);

    //x.AddServiceBusMessageScheduler(); //allow sending messages in future/delayed on service bus
    x.UsingAzureServiceBus((context, cfg) =>
    {
        //cfg.UseServiceBusMessageScheduler();
        cfg.Host(
            "Endpoint=sb://platform-experiment.servicebus.windows.net/;SharedAccessKeyName=platform-experiment;SharedAccessKey=ARSa58de7yen1D5PeFkWwzB7srsPo7+PSD3H1vpFuZE=");
        cfg.ConfigureEndpoints(context);
    });
});
*/




builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.EnsureDatabaseCreated();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
