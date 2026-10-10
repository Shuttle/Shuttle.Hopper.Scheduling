using Shuttle.Contract;
using Shuttle.Cron;

namespace Shuttle.Hopper.Scheduling;

public class Schedule
{
    private CronExpression _cronExpression;

    public Schedule(Guid id, string name, string cronExpression, DateTime? nextNotification, string? inboxWorkQueueUri)
    {
        Id = id;
        Name = Guard.AgainstEmpty(name);
        CronExpression = Guard.AgainstEmpty(cronExpression);
        _cronExpression = new(CronExpression);
        NextNotification = nextNotification ?? _cronExpression.NextOccurrence();
        InboxWorkQueueUri = inboxWorkQueueUri;
    }

    public string CronExpression { get; private set; }

    public Guid Id { get; }

    public string? InboxWorkQueueUri { get; private set; }

    public string Name { get; private set; }
    public DateTime NextNotification { get; private set; }
    public bool ShouldPublish => string.IsNullOrWhiteSpace(InboxWorkQueueUri);

    public bool ShouldSendNotification => DateTime.UtcNow >= NextNotification;

    public void SetCronExpression(string cronExpression)
    {
        CronExpression = Guard.AgainstEmpty(cronExpression);
        _cronExpression = new(CronExpression);
        NextNotification = _cronExpression.NextOccurrence();
    }

    public void SetInboxWorkQueueUri(string? inboxWorkQueueUri)
    {
        InboxWorkQueueUri = inboxWorkQueueUri;
    }

    public void SetName(string name)
    {
        Name = Guard.AgainstEmpty(name);
    }

    public void SetNextNotification()
    {
        NextNotification = _cronExpression.NextOccurrence();
    }

    public class Specification
    {
        public List<Guid> Ids { get; } = [];
        public string NameMatch { get; private set; } = string.Empty;
        public DateTime? NextNotificationFromInclusive { get; private set; }
        public DateTime? NextNotificationToExclusive { get; private set; }

        public Specification AddId(Guid id)
        {
            Ids.Add(id);

            return this;
        }

        public Specification AddIds(IEnumerable<Guid> ids)
        {
            Ids.AddRange(Guard.AgainstNull(ids));

            return this;
        }

        public Specification WithNameMatch(string nameMatch)
        {
            NameMatch = Guard.AgainstNull(nameMatch);

            return this;
        }

        public Specification WithNextNotificationFromInclusive(DateTime value)
        {
            NextNotificationFromInclusive = value;

            return this;
        }

        public Specification WithNextNotificationToExclusive(DateTime value)
        {
            NextNotificationToExclusive = value;

            return this;
        }
    }
}