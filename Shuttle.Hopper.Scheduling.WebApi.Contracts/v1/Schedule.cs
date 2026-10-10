namespace Shuttle.Hopper.Scheduling.WebApi.Contracts.v1;

public class Schedule
{
    public string CronExpression { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public string? InboxWorkQueueUri { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime NextNotification { get; set; }

    public class Specification
    {
        public List<Guid> Ids { get; set; } = [];
        public string NameMatch { get; set; } = string.Empty;
        public DateTime? NextNotificationFromInclusive { get; set; }
        public DateTime? NextNotificationToExclusive { get; set; }
    }
}
