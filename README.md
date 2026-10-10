# Shuttle.Hopper.Scheduling

`Shuttle.Hopper.Scheduling` is a cron-based scheduling service built on top of the [`Shuttle.Hopper`](https://github.com/Shuttle/Shuttle.Hopper) message bus. Schedules are persisted, evaluated against a cron expression, and — when due — dispatched as `ScheduleNotification` messages over the bus for interested endpoints to consume.

## Solution layout

| Project | Description |
|---|---|
| `Shuttle.Hopper.Scheduling` | Core abstractions: the `Schedule` model, `IScheduleRepository`, and the `AddScheduling()` registration extension. |
| `Shuttle.Hopper.Scheduling.SqlServer` | Entity Framework Core / SQL Server implementation of `IScheduleRepository`, including migrations. |
| `Shuttle.Hopper.Scheduling.Messages` | The `ScheduleNotification` message contract, referenced by projects that consume schedule notifications. |
| `Shuttle.Hopper.Scheduling.Server` | A worker service host that polls due schedules and publishes/sends `ScheduleNotification` messages via `Shuttle.Hopper`. |
| `Shuttle.Hopper.Scheduling.WebApi` | A minimal-API web service for managing schedules (create, search, update, remove). |
| `Shuttle.Hopper.Scheduling.WebApi.Contracts` | The `v1` request/response DTOs used by `Shuttle.Hopper.Scheduling.WebApi`. |

## Installation

```bash
dotnet add package Shuttle.Hopper.Scheduling
```

## Registration

Register scheduling with `AddScheduling()`, then choose a storage implementation such as `UseSqlServer()` (from `Shuttle.Hopper.Scheduling.SqlServer`):

```csharp
services
    .AddScheduling()
    .UseSqlServer(options =>
    {
        configuration.GetSection(SchedulingSqlServerOptions.SectionName).Bind(options);

        options.ConnectionString = configuration.GetConnectionString("Automation")
            ?? throw new ApplicationException("Missing connection string 'Automation'.");
    });
```

`AddScheduling()` returns a `SchedulingBuilder` exposing the underlying `IServiceCollection`, and `UseSqlServer()` registers `IScheduleRepository`, `SchedulingDbContext`, and the options validator needed to run against SQL Server.

## The `Schedule` model

Each `Schedule` combines a name, a standard cron expression, and an optional `InboxWorkQueueUri`:

```csharp
public class Schedule
{
    public Guid Id { get; }
    public string Name { get; }
    public string CronExpression { get; }
    public DateTime NextNotification { get; }
    public string? InboxWorkQueueUri { get; }
}
```

- `CronExpression` is parsed with [`Shuttle.Cron`](https://github.com/Shuttle/Shuttle.Cron) to determine `NextNotification`.
- `ShouldSendNotification` is `true` once `NextNotification` has elapsed.
- `ShouldPublish` is `true` when `InboxWorkQueueUri` is empty — otherwise the notification is sent directly to that queue instead of being published.
- `SetName`, `SetCronExpression`, and `SetInboxWorkQueueUri` mutate the schedule (re-calculating `NextNotification` when the cron expression changes); `SetNextNotification` advances `NextNotification` using the current cron expression.

`IScheduleRepository` provides `AddAsync`, `FindAsync`, `RemoveAsync`, `SaveAsync`, `SearchAsync` (accepting a fluent `Schedule.Specification` — `AddId(s)`, `WithNameMatch`, `WithNextNotificationFromInclusive`, `WithNextNotificationToExclusive`), and `AllAsync`/`SetNextNotificationAsync` (used by the `Server` poll loop).

## SQL Server storage

`Shuttle.Hopper.Scheduling.SqlServer` binds `SchedulingSqlServerOptions` from configuration:

```json
{
  "Shuttle": {
    "Hopper": {
      "Scheduling": {
        "SqlServer": {
          "Schema": "scheduling",
          "CommandTimeout": "00:01:00"
        }
      }
    }
  }
}
```

| Property | Default | Description |
|---|---|---|
| `ConnectionString` | `Automation` | Name of the connection string (resolved via `IConfiguration.GetConnectionString`) used by the `SchedulingDbContext`. |
| `Schema` | `scheduling` | Default database schema for the scheduling tables. |
| `CommandTimeout` | `00:01:00` | Command timeout applied to the underlying SQL Server provider. |

`SectionName` (`Shuttle:Hopper:Scheduling:SqlServer`) identifies the configuration section above. Migrations are included in the project and can be applied with the standard EF Core tooling, e.g.:

```bash
dotnet ef database update --project Shuttle.Hopper.Scheduling.SqlServer
```

## `Shuttle.Hopper.Scheduling.Server`

The `Server` project hosts a `Scheduler` background service (`IHostedService`) that, every 5 seconds:

1. Loads all schedules via `IScheduleRepository`.
2. For each schedule where `ShouldSendNotification` is `true`, dispatches a `ScheduleNotification`:
   - **Publishes** the notification via `IBus.PublishAsync` when `ShouldPublish` is `true` (no `InboxWorkQueueUri` configured), so any subscribed endpoint receives it.
   - Otherwise **sends** the notification via `IBus.SendAsync` directly to the schedule's `InboxWorkQueueUri`.
3. Advances `NextNotification` using the cron expression and persists it back through `IScheduleRepository.SetNextNotificationAsync`.

The host is wired up with `Shuttle.Hopper` and the Azure Storage Queues transport (`Shuttle.Hopper.AzureStorageQueues`) in `Program.cs`, and requires a `CONFIGURATION_FOLDER` environment variable pointing at a directory containing its `appsettings.json`.

## `Shuttle.Hopper.Scheduling.Messages`

Consumers that need to react to due schedules reference this package for the `ScheduleNotification` (`v1`) message:

```csharp
namespace Shuttle.Hopper.Scheduling.Messages.v1;

public class ScheduleNotification
{
    public string Name { get; set; }
    public string CronExpression { get; set; }
    public DateTime NextNotification { get; set; }
}
```

Handle it like any other `Shuttle.Hopper` message, e.g. via `IMessageHandler<ScheduleNotification>` or `IContextMessageHandler<ScheduleNotification>`.

## `Shuttle.Hopper.Scheduling.WebApi`

A minimal-API web service, built on the same conventions as `Shuttle.Access.WebApi` / `Shuttle.Recall.WebApi`, for managing schedules directly against `IScheduleRepository` (create, search, look up, edit, remove — no message bus is required to run it). Authorization is delegated to a deployed `Shuttle.Access` instance via `Shuttle.Access.AspNetCore` (`AddAccessAuthorization`, `UseAccessAuthorization`, `.RequireSession()`), following the same pattern as `Shuttle.Recall.WebApi`.

Endpoints (all under `/v1`, tagged `Schedules`):

| Method | Route | Description |
|---|---|---|
| `POST` | `/schedules/search` | Search using a `Schedule.Specification` (`Ids`, `NameMatch`, `NextNotificationFromInclusive`, `NextNotificationToExclusive`). |
| `GET` | `/schedules/{id}` | Get a single schedule by id. |
| `POST` | `/schedules` | Register a new schedule (`RegisterSchedule`). |
| `DELETE` | `/schedules/{id}` | Remove a schedule. |
| `PATCH` | `/schedules/{id}/name` | Set the schedule's name (`SetName`). |
| `PATCH` | `/schedules/{id}/cron-expression` | Set the schedule's cron expression, recalculating `NextNotification` (`SetCronExpression`). |
| `PATCH` | `/schedules/{id}/inbox-work-queue-uri` | Set (or clear) the schedule's `InboxWorkQueueUri` (`SetInboxWorkQueueUri`). |

Plus a `GET /v1/server/configuration` endpoint reporting the assembly version.

It requires a `CONFIGURATION_FOLDER` environment variable pointing at a directory containing its `appsettings.json`, with `ConnectionStrings:Automation`, `Shuttle:Access:Authorization:BaseAddress` (the `Shuttle.Access` deployment used to resolve sessions), and `Shuttle:Hopper:Scheduling:SqlServer` sections configured. API documentation is served via Scalar at `/scalar/v1`.

## `Shuttle.Hopper.Scheduling.WebApi.Contracts`

The `v1` request/response DTOs (`Schedule`, `Schedule.Specification`, `RegisterSchedule`, `SetName`, `SetCronExpression`, `SetInboxWorkQueueUri`) used by `Shuttle.Hopper.Scheduling.WebApi`, published as a standalone package so clients can depend on the contracts without pulling in the web host.

Handle it like any other `Shuttle.Hopper` message, e.g. via `IMessageHandler<ScheduleNotification>` or `IContextMessageHandler<ScheduleNotification>`.
