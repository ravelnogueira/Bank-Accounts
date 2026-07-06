using Bank.Accounts.Application.Common.Clock;

namespace Bank.Accounts.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

