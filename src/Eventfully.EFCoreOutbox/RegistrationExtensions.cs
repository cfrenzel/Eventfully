using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Eventfully.Outboxing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Eventfully.EFCoreOutbox;

namespace Eventfully
{
    public static class RegistrationExtensions
    {
        public static void WithEFCoreOutbox<T>(this IServiceRegistrar services,
            Action<EFCoreOutbox.OutboxSettings> config
        ) where T : DbContext
        {
            OutboxSettings settings = new OutboxSettings();
            if (config != null)
                config.Invoke(settings);
            services.AddSingleton<OutboxSettings>(settings);
            services.AddSingleton<IOutbox, Outbox<T>>(true);
            services.AddTransient<IOutboxSession, OutboxSession<T>>();
        }
    }
}
