namespace Shuttle.Hopper.Scheduling.WebApi.Contracts.v1;

public class RegisterSchedule
{
    public string CronExpression { get; set; } = string.Empty;
    public Guid? Id { get; set; }
    public string? InboxWorkQueueUri { get; set; }
    public string Name { get; set; } = string.Empty;
}
