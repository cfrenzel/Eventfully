using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OrderingApi.Entities;

namespace OrderingApi
{
    
    public class ApplicationDbContext : SagaDbContext //DbContext
    {

        public DbSet<Order> Orders { get; set; }

        protected override IEnumerable<ISagaClassMap> Configurations
        {
            get { yield return new OrderStateMap(); }
        }
        
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            
            
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.AddOutboxMessageEntity();
            builder.AddOutboxStateEntity();
            builder.AddInboxStateEntity();
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.MapStringEnumerationClass<DeliveryMethod>();
            configurationBuilder.MapStringEnumerationClass<PaymentStyle>();
        }

        public override int SaveChanges()
        {
            _preSaveChanges();
            var res = base.SaveChanges();
            return res;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            _preSaveChanges();
            var res = await base.SaveChangesAsync(cancellationToken);
            return res;
        }

        private void _preSaveChanges()
        {
            _addDateTimeStamps();
        }


        private void _addDateTimeStamps()
        {
            foreach (var item in ChangeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
            {
                var now = DateTime.UtcNow;

                if (item.State == EntityState.Added && item.Metadata.FindProperty("CreatedAt") != null)
                {
                    var prop = item.Property("CreatedAt");
                    if((DateTime)prop.CurrentValue == default(DateTime))
                        prop.CurrentValue = now;
                }
                else if (item.State == EntityState.Added && item.Metadata.FindProperty("CreatedAtUtc") != null)
                {
                    var prop = item.Property("CreatedAtUtc");
                    if ((DateTime)prop.CurrentValue == default(DateTime))
                        prop.CurrentValue = now;
                }

                //if (item.Metadata.FindProperty("UpdatedAt") != null)
                //    item.Property("UpdatedAt").CurrentValue = now;
                //else 
                if (item.Metadata.FindProperty("UpdatedAtUtc") != null)
                    item.Property("UpdatedAtUtc").CurrentValue = now;
            }
        }
    }
    
    public class EnumerationStringConverter<TEnum> : ValueConverter<TEnum, string>
        where TEnum : Enumeration<TEnum, string>
    {
        public EnumerationStringConverter()
            : base(
                v => v.Value,
                v => Enumeration<TEnum,string>.Parse(v))
        {}
    }
   
    public static class EnumerationConfiguration
    {
        public static ModelConfigurationBuilder MapStringEnumerationClass<TEnum>(this ModelConfigurationBuilder builder)
            where TEnum : Enumeration<TEnum, string>
        {
            builder
                .Properties<TEnum>()
                .HaveConversion<EnumerationStringConverter<TEnum>>()
                .HaveMaxLength(50);
            return builder;
        }
    }
    
}