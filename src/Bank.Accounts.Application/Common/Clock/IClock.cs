namespace Bank.Accounts.Application.Common.Clock;

public interface IClock
{
    DateTime UtcNow { get; }
}

