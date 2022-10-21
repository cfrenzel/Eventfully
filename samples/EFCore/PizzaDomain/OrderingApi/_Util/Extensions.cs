using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OrderingApi
{
    public static class Extensions
    {
        public static async Task EnsureDatabaseCreated(this IHost host)
        {
            using (var scope = host.Services.GetService<IServiceScopeFactory>().CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await db.Database.MigrateAsync();
            }
        }
    }
}