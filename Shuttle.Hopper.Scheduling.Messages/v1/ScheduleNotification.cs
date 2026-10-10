namespace Shuttle.Hopper.Scheduling.Messages.v1;

public class ScheduleNotification
{
    public string CronExpression { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime NextNotification { get; set; }
}