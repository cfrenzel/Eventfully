using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Eventfully.Outboxing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Eventfully.EFCoreOutbox
{
    public static class MicrosoftDependencyInjectionExtensions
    {
        public static void AddEFCoreOutbox<T>(this IServiceCollection services,
            Action<EFCoreOutbox.OutboxSettings> config
        ) where T : DbContext
        {

            OutboxSettings settings = new OutboxSettings();
            if (config != null)
                config.Invoke(settings);

            services.AddSingleton(settings);
            services.AddSingleton<Outbox<T>>();
            
            //register the same outbox as IOutbox
            services.AddSingleton<IOutbox>(x =>
                x.GetRequiredService<Outbox<T>>()
            );
     
            //register the outboxsession as transient
            services.AddTransient<IOutboxSession, OutboxSession<T>>();
           


        }
    }
}
