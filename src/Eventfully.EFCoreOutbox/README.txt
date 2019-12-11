 - Add property to your DbContext
        
        public DbSet<Common.Infrastructure.Messaging.EFCore.OutboxEvent> OutboxEvents { get; set; }
     
  
 - Add to your DbContext OnModelCreating
     
        builder.ApplyConfiguration(new Common.Infrastructure.Messaging.EFCore.OutboxEventEntityConfiguration());
        builder.ApplyConfiguration(new Common.Infrastructure.Messaging.EFCore.OutboxEventDataEntityConfiguration());


 - If using migrations you can

  Add-Migration Outbox


 - Configure Depentcy Injection


			//Needed for reading/updating/deleting  the outbox
            services.AddTransient<IFactory<IDbConnection>>(x =>
                new Common.Infrastructure.SqlHelper.SqlConnectionFactory(_config.GetConnectionString("ApplicationConnection"))
            );

			//needed for publishing the events from the outbox
            services.AddScoped<IEventPublisher, Application.IntegrationEventPublisher>();

			//needed for injecting the outbox into the DbContext and the OutboxEventPumpService
            services.AddScoped<Common.Infrastructure.Messaging.EFCore.Outbox>();

			//needed for injecting into application layer handlers
            services.AddScoped<IEventOutbox>(x => 
                x.GetRequiredService<Common.Infrastructure.Messaging.EFCore.Outbox>()
            );

			//needed to start the event pump (can be done anywhere, doesn't have to be in a webapp)
            services.AddHostedService<Common.Infrastructure.Messaging.EFCore.OutboxEventPumpService>();
        


