using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Eventfully.Outboxing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Eventfully.EFCoreOutbox;

namespace Eventfully.EFCoreOutbox
{
    public static class EFCoreConfigurationExtensions
    {
        public static void UseEFCore<T>(this MessagingConfiguration messagingConfig, string connectionString,
            Action<EFCoreOutbox.OutboxSettings> config
        ) where T : DbContext
        {
            OutboxSettings settings = new OutboxSettings(connectionString);
            config?.Invoke(settings);
            messagingConfig.Services.AddSingleton<OutboxSettings>(settings);
            messagingConfig.Services.AddSingleton<IOutbox, Outbox<T>>(true);
            messagingConfig.Services.AddTransient<IOutboxSession, OutboxSession<T>>();
        }
    }
}
