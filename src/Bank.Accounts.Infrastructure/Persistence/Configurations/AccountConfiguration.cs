using Bank.Accounts.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bank.Accounts.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(account => account.Id);
        builder.Property(account => account.HolderName).HasMaxLength(150).IsRequired();
        builder.Property(account => account.TaxId).HasMaxLength(11).IsRequired();
        builder.Property(account => account.Status).HasConversion<int>().IsRequired();
        builder.Property(account => account.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(account => account.UpdatedAt).HasColumnType("timestamp with time zone");
        builder.Property(account => account.DeletedAt).HasColumnType("timestamp with time zone");
        builder.Ignore(account => account.DomainEvents);

        builder.HasIndex(account => account.TaxId)
            .IsUnique()
            .HasDatabaseName("IX_Accounts_TaxId_NotDeleted")
            .HasFilter("\"DeletedAt\" IS NULL");
        builder.HasIndex(account => account.Status)
            .HasDatabaseName("IX_Accounts_Status");
        builder.HasIndex(account => account.DeletedAt)
            .HasDatabaseName("IX_Accounts_DeletedAt");

        builder.HasQueryFilter(account => account.DeletedAt == null);
    }
}

