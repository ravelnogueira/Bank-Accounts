using Bank.Accounts.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.Accounts.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.EventType).HasMaxLength(200).IsRequired();
        builder.Property(message => message.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(message => message.OccurredAt).HasColumnType("timestamp with time zone");
        builder.Property(message => message.ProcessedAt).HasColumnType("timestamp with time zone");
        builder.Property(message => message.Error).HasMaxLength(4000);
        builder.Property(message => message.Status).HasConversion<int>().IsRequired();
        builder.HasIndex(message => new { message.Status, message.OccurredAt })
            .HasDatabaseName("IX_OutboxMessages_Status_OccurredAt");
    }
}

