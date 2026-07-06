using Npgsql;
using Bank.Accounts.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Bank.Accounts.Application.Common.Errors;
using Bank.Accounts.Application.Accounts.Interfaces;

namespace Bank.Accounts.Infrastructure.Persistence;

public sealed class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    public async Task AddOutboxMessagesAsync(IReadOnlyCollection<OutboxMessage> messages,
        CancellationToken cancellationToken) => await dbContext.OutboxMessages.AddRangeAsync(messages, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_Accounts_TaxId_NotDeleted"
            })
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new TaxIdAlreadyExistsException();
        }
    }
}

