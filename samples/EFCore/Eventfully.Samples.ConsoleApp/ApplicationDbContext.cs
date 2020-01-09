using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Eventfully;
using Eventfully.EFCoreOutbox;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Eventfully.Samples.ConsoleApp
{
    public class ApplicationDbContext : DbContext, ISupportTransientDispatch
    {
        public event EventHandler ChangesPersisted;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {}


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            /*** OUTBOX Message Entities (see Messaging.EFCoreOutbox) ***/
            builder.AddEFCoreOutbox();
        }

        public override int SaveChanges()
        {
            _preSaveChanges();
            var res = base.SaveChanges();
            _postSaveChanges();
            return res;
        }
      
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            _preSaveChanges();
            var res = await base.SaveChangesAsync(cancellationToken);
            _postSaveChanges();
            return res;
        }

        private void _preSaveChanges()
        {
            _addDateTimeStamps();
        }
        private void _postSaveChanges()
        {
            this.ChangesPersisted?.Invoke(this, null);
        }


        private void _addDateTimeStamps()
        {
            foreach (var item in ChangeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
            {
                var now = DateTime.UtcNow;

                if (item.State == EntityState.Added && item.Metadata.FindProperty("CreatedAt") != null)
                    item.Property("CreatedAt").CurrentValue = now;
                else if (item.State == EntityState.Added && item.Metadata.FindProperty("CreatedAtUtc") != null)
                    item.Property("CreatedAtUtc").CurrentValue = now;

                if (item.Metadata.FindProperty("UpdatedAt") != null)
                    item.Property("UpdatedAt").CurrentValue = now;
                else if (item.Metadata.FindProperty("UpdatedAtUtc") != null)
                    item.Property("UpdatedAtUtc").CurrentValue = now;
            }
        }

    }


  

}


