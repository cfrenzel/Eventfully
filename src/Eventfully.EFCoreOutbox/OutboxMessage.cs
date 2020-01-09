using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Eventfully.EFCoreOutbox
{
    /// <summary>
    /// OutboxMessage and OutboxEventData have a true 1-to-1 mapping (share a primary key)
    /// </summary>
    public class OutboxMessage
    {
        public Guid Id { get; private set; }
        public DateTime PriorityDateUtc { get; set; }
        public int TryCount { get; set; }

        [StringLength(500),Required]
        public string Type { get; set; }

        [StringLength(500)]
        public string Endpoint { get; set; }

        public int Status { get; set; }

        public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
        public DateTime? ExpiresAtUtc { get; set; }

        public virtual OutboxMessageData MessageData { get; set; }


        /// <summary>
        /// After the events are commited to the outbox
        /// they are published immediately before the outbox monitor begins working
        /// this field will cause this immediate publishing to be skipped
        /// this makes since when publishing a large number or events
        /// or looping back to handle events asynchronously in the same application
        /// </summary>
        [NotMapped]
        public bool SkipTransientDispatch { get; private set; } = false;

        private OutboxMessage() {
        }

        public OutboxMessage(string type, byte[] messageData, string messageMetaData, DateTime tryAtUtc, bool skipTransientDispatch = false, DateTime? expiresAtUtc = null) :
            this(type, messageData, messageMetaData, tryAtUtc, null, skipTransientDispatch, expiresAtUtc)
        { }

        public OutboxMessage(string type, byte[] messageData, string messageMetaData, DateTime tryAtUtc, string endpoint, bool skipTransientDispatch = false, DateTime? expiresAtUtc = null)
        {
            this.Id = MassTransit.NewId.NextGuid();
            this.Type = type;
            this.MessageData = new OutboxMessageData(this.Id, messageData, messageMetaData);
            this.PriorityDateUtc = tryAtUtc;
            this.Endpoint = endpoint;
            this.SkipTransientDispatch = skipTransientDispatch;
            this.ExpiresAtUtc = expiresAtUtc;

            //prevent the transient dispatcher from competing with the outbox dispatcher
            if (!this.SkipTransientDispatch)
                this.Status = (int)OutboxMessageStatus.InProgress;
          }

        public bool IsExpired(DateTime utcNow)
        {
            if (this.ExpiresAtUtc.HasValue && this.ExpiresAtUtc.Value <= utcNow)
                return true;
            return false;
        }

        public class OutboxMessageData
        {
            public Guid Id { get;  set; }

            [Required]
            public byte[] Data { get; set; }
            public string MetaData { get; set; }

            public OutboxMessageData() { }
            public OutboxMessageData(Guid id, byte[] data, string metaData)
            {
                this.Id = id;
                this.Data = data;
                this.MetaData = metaData;
            }
        }
    }


    public class OutboxMessageEntityConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OutboxMessages");//, SchemaNames.Application);
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Id).ValueGeneratedNever();

            //setup a true 1-to-1
            builder.HasOne(x => x.MessageData).WithOne().HasForeignKey<OutboxMessage.OutboxMessageData>(x => x.Id);


            builder.ForSqlServerHasIndex(p => new { p.PriorityDateUtc, p.Status })
                .ForSqlServerInclude(p => new { p.TryCount, p.Type, p.ExpiresAtUtc, p.CreatedAtUtc, p.Endpoint })//"[TryCount]","[Type]","[ExpiresAtUtc]","[CreatedAtUtc]","[Endpoint]")
                .HasName("IX_PriorityDateUtc");
       
            //builder.HasIndex(p => new { p.PriorityDateUtc, p.Status })
            //    .HasAnnotation("SqlServer:IncludeIndex", "[TryCount],[Type],[ExpiresAtUtc],[CreatedAtUtc],[Endpoint]")
            //    .HasName("IX_PriorityDateUtc");
        }
    }

    public class OutboxMessageDataEntityConfiguration : IEntityTypeConfiguration<OutboxMessage.OutboxMessageData>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage.OutboxMessageData> builder)
        {
            builder.ToTable("OutboxMessageData");
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Id).ValueGeneratedNever();
        }
    }

    public static class EFCoreOutboxExtensions
    {
        /// <summary>
        /// Helper to add the Outbox entities to the DbContext
        /// 
        /// //Add to your DbContext
        ///  protected override void OnModelCreating(ModelBuilder builder)
        /// {
        ///    base.OnModelCreating(builder);
        ///    builder.AddEFCoreOutbox();
        /// }
        /// </summary>
        /// <param name="builder"></param>
        public static void AddEFCoreOutbox(this ModelBuilder builder)
        {
            builder.ApplyConfiguration(new OutboxMessageEntityConfiguration());
            builder.ApplyConfiguration(new OutboxMessageDataEntityConfiguration());
        }
    }
}
