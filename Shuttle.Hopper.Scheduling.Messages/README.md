# Shuttle.Hopper.Scheduling.Messages

```
PM> Install-Package Shuttle.Hopper.Scheduling.Messages
```

You would typically reference this package in a project that is going to receive messages from the Shuttle.Hopper.Scheduling.Server endpoint.

## `ScheduleNotification` (`v1`)

Raised when a `Schedule`'s cron expression comes due. Depending on the schedule's configuration, `Shuttle.Hopper.Scheduling.Server` either publishes this message to all subscribers or sends it directly to the schedule's inbox work queue.

```csharp
namespace Shuttle.Hopper.Scheduling.Messages.v1;

public class ScheduleNotification
{
    public string Name { get; set; }
    public string CronExpression { get; set; }
    public DateTime NextNotification { get; set; }
}
```

| Property | Description |
|---|---|
| `Name` | The name of the schedule that triggered the notification. |
| `CronExpression` | The cron expression that determined when the notification was due. |
| `NextNotification` | The (UTC) occurrence time that triggered this notification. |

Handle it like any other `Shuttle.Hopper` message, e.g. via `IMessageHandler<ScheduleNotification>` or `IContextMessageHandler<ScheduleNotification>`.

Please take a look at the [repository](https://github.com/Shuttle/Shuttle.Hopper.Scheduling) for more information.