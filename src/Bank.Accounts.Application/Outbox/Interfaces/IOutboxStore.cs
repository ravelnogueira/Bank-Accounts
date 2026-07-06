namespace Bank.Accounts.Application.Outbox.Interfaces;

public interface IOutboxStore
{
    Task<IOutboxBatch> ClaimPendingAsync(int batchSize, CancellationToken cancellationToken);
}
