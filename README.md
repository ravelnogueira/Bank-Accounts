# Bank Accounts API

.NET 8 REST API for bank account management. The solution uses ASP.NET Core MVC, Clean Architecture, PostgreSQL, Redis, RabbitMQ, and the transactional Outbox Pattern.

## Architecture

```text
src/
  Bank.Accounts.Api             MVC controllers, middleware, OpenAPI
  Bank.Accounts.Application     Use cases, DTOs, validation, ports
  Bank.Accounts.Domain          Account rules, events, outbox state
  Bank.Accounts.Infrastructure  EF Core, Redis, RabbitMQ, worker, health
tests/
  Bank.Accounts.UnitTests       Isolated unit tests and fakes
```

Dependencies point inward: Infrastructure and API depend on Application and Domain. Domain has no external package dependencies.

No integration-test project is included by design.

## Prerequisites

- .NET 8 SDK
- EF Core CLI (`dotnet tool install --global dotnet-ef`)
- Docker with Docker Compose

## Run locally

The Docker Compose and Development settings use local-only credentials to make the technical test easy to run. Do not reuse these values in production.

Start PostgreSQL, Redis, and RabbitMQ:

```powershell
docker compose up -d
```

The local ports are:

```text
PostgreSQL  localhost:5432
Redis       localhost:6380
RabbitMQ    localhost:5672
RabbitMQ UI http://localhost:15672
```

Local credentials:

```text
PostgreSQL database: bank_accounts
PostgreSQL user:     postgres
PostgreSQL password: postgres
RabbitMQ user:       guest
RabbitMQ password:   guest
```

Restore packages and apply the database migration:

```powershell
dotnet restore
dotnet ef database update `
  --project src/Bank.Accounts.Infrastructure `
  --startup-project src/Bank.Accounts.Api
```

Run the API:

```bash
dotnet run --project src/Bank.Accounts.Api
```

In Development, Swagger is available at `/swagger`. RabbitMQ management is available at `http://localhost:15672`.

If PostgreSQL was already created with different local credentials, recreate the containers and volume:

```powershell
docker compose down -v
docker compose up -d
```

Run unit tests:

```bash
dotnet test
```

## Endpoints

```text
POST   /api/accounts
GET    /api/accounts/{id}
GET    /api/accounts?taxId=&status=&page=&pageSize=
PUT    /api/accounts/{id}
DELETE /api/accounts/{id}
GET    /health
```

The list endpoint requires valid pagination. Defaults are page 1 and page size 20; the maximum page size is 100. Deleted accounts are excluded by an EF Core global query filter.

## Transactional Outbox

Create, update, and delete operations add an `OutboxMessage` to the same EF Core unit of work as the account change. A database transaction commits both records atomically. The HTTP request never publishes directly to RabbitMQ.

`OutboxProcessorService` polls every five seconds. Each cycle:

1. claims up to 20 pending rows with `FOR UPDATE SKIP LOCKED`;
2. publishes each event to the durable `bank.accounts` topic exchange and waits for broker confirmation;
3. uses the outbox ID as the RabbitMQ message ID;
4. marks successful messages as processed;
5. records an error and increments the retry count after a failure;
6. marks a message failed after five attempts.

Delivery is at least once. A crash after broker publication but before the database commit may cause redelivery, so consumers must deduplicate by message ID.

Broker unavailability does not affect account writes after the account/outbox transaction commits. The worker retries later.

## RabbitMQ demo queue

The API is a producer. It declares and publishes to the `bank.accounts` topic exchange, but it does not own consumer queues. In a real integration, each consuming area creates its own queue and binding.

For local testing, create a demo queue bound to all account events:

```powershell
$auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("guest:guest"))
$headers = @{ Authorization = "Basic $auth" }

Invoke-RestMethod `
  -Method Put `
  -Uri "http://localhost:15672/api/queues/%2f/bank.accounts.demo" `
  -Headers $headers `
  -ContentType "application/json" `
  -Body '{"durable":true,"arguments":{}}'

Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:15672/api/bindings/%2f/e/bank.accounts/q/bank.accounts.demo" `
  -Headers $headers `
  -ContentType "application/json" `
  -Body '{"routing_key":"#","arguments":{}}'
```

Then create, update, or delete an account and inspect `bank.accounts.demo` in the RabbitMQ management UI at `http://localhost:15672`, or fetch messages with:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:15672/api/queues/%2f/bank.accounts.demo/get" `
  -Headers $headers `
  -ContentType "application/json" `
  -Body '{"count":10,"ackmode":"ack_requeue_false","encoding":"auto","truncate":50000}'
```

## Operational alerts

Attempts one through four produce structured `Warning` logs. The fifth failure produces a `Critical` log with event ID `5001 / OUTBOX_MESSAGE_FAILED`.

`GET /health` reports the Outbox as degraded when at least one failed message exists or the pending backlog exceeds 100. Alert delivery is deliberately delegated to the monitoring platform (for example Datadog, CloudWatch, or Prometheus/Alertmanager), which can route it to Teams, Slack, or PagerDuty without coupling application code to a vendor.

Failed rows remain in PostgreSQL for diagnosis and controlled manual reprocessing.

## Redis cache

The API uses cache-aside for lookups by account ID and Tax ID:

```text
account:{id}
account:tax-id:{taxId}
```

Entries expire after twelve hours. A cache miss reads PostgreSQL and populates both keys. Update and delete invalidate both keys. Paged lists are not cached because their invalidation cost is disproportionate to this use case. PostgreSQL is always the source of truth.

Redis read failures fall back to PostgreSQL. A cache invalidation failure is logged after the account transaction succeeds and does not roll back persisted data; the short TTL limits the stale-data window.

## Errors and correlation IDs

Errors use `application/problem+json` and include:

```json
{
  "type": "https://httpstatuses.com/409",
  "title": "Conflict",
  "status": 409,
  "detail": "An account with this Tax ID already exists.",
  "code": "ACCOUNT_TAX_ID_ALREADY_EXISTS",
  "traceId": "client-or-generated-correlation-id"
}
```

Stable codes include `VALIDATION_ERROR`, `ACCOUNT_NOT_FOUND`, `ACCOUNT_TAX_ID_ALREADY_EXISTS`, and `INTERNAL_SERVER_ERROR`.

Clients may send `X-Correlation-Id`. The API generates one when absent, returns it in the response header, adds it to the logging scope, and includes it as `traceId` in errors.

Tax ID values are masked before logging. Event payloads are not logged.

## Database

The initial migration creates:

- `Accounts`, including soft-delete timestamps;
- a partial unique index on Tax ID for rows where `DeletedAt IS NULL`;
- indexes on status and deletion timestamp;
- `OutboxMessages` and its polling index.

Soft deletion sets `DeletedAt`, updates `UpdatedAt`, and changes status to `Inactive`.

## Trade-offs

- Outbox provides atomic database persistence and durable asynchronous publication, but adds a table, polling worker, retries, and retention requirements.
- Holding row locks while publishing prevents concurrent workers from processing the same row. It also keeps transactions open during broker calls; the batch is limited to 20 to bound this cost.
- At-least-once delivery favors durability over exactly-once claims. Consumers must be idempotent.
- Redis reduces repeated reads but can briefly serve stale data if invalidation fails. The twelve-hour TTL bounds that risk.
- Tax ID check digits are validated before persistence. Uniqueness is checked before insert for a clear error and enforced by PostgreSQL to close race conditions.
- Direct vendor notifications are excluded. Critical logs and health degradation create a portable alert signal without making the API depend on an incident-management provider.

