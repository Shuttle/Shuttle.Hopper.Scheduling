using NUnit.Framework;
using Shuttle.Cron;

namespace Shuttle.Hopper.Scheduling.Tests;

[TestFixture]
public class ScheduleFixture
{
    private const string EveryMinuteCronExpression = "* * * * *";

    [Test]
    public void Should_throw_when_name_is_empty()
    {
        Assert.That(() => new Schedule(Guid.NewGuid(), string.Empty, EveryMinuteCronExpression, null, null), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void Should_throw_when_cron_expression_is_empty()
    {
        Assert.That(() => new Schedule(Guid.NewGuid(), "schedule-name", string.Empty, null, null), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void Should_throw_when_cron_expression_is_invalid()
    {
        Assert.That(() => new Schedule(Guid.NewGuid(), "schedule-name", "invalid", null, null), Throws.TypeOf<CronException>());
    }

    [Test]
    public void Should_be_able_to_create_a_new_schedule()
    {
        var id = Guid.NewGuid();

        var schedule = new Schedule(id, "schedule-name", EveryMinuteCronExpression, null, "queue://work");

        Assert.That(schedule.Id, Is.EqualTo(id));
        Assert.That(schedule.Name, Is.EqualTo("schedule-name"));
        Assert.That(schedule.CronExpression, Is.EqualTo(EveryMinuteCronExpression));
        Assert.That(schedule.InboxWorkQueueUri, Is.EqualTo("queue://work"));
    }

    [Test]
    public void Should_calculate_the_next_notification_when_none_is_specified()
    {
        var before = DateTime.UtcNow;

        var schedule = new Schedule(Guid.NewGuid(), "schedule-name", EveryMinuteCronExpression, null, null);

        Assert.That(schedule.NextNotification, Is.GreaterThan(before));
    }

    [Test]
    public void Should_use_the_given_next_notification_when_specified()
    {
        var nextNotification = DateTime.UtcNow.AddDays(1);

        var schedule = new Schedule(Guid.NewGuid(), "schedule-name", EveryMinuteCronExpression, nextNotification, null);

        Assert.That(schedule.NextNotification, Is.EqualTo(nextNotification));
    }

    [Test]
    public void Should_indicate_that_a_notification_should_be_sent_when_the_next_notification_has_elapsed()
    {
        var schedule = new Schedule(Guid.NewGuid(), "schedule-name", EveryMinuteCronExpression, DateTime.UtcNow.AddMinutes(-1), null);

        Assert.That(schedule.ShouldSendNotification, Is.True);
    }

    [Test]
    public void Should_indicate_that_a_notification_should_not_be_sent_when_the_next_notification_is_in_the_future()
    {
        var schedule = new Schedule(Guid.NewGuid(), "schedule-name", EveryMinuteCronExpression, DateTime.UtcNow.AddDays(1), null);

        Assert.That(schedule.ShouldSendNotification, Is.False);
    }

    [Test]
    public void Should_indicate_that_a_notification_should_be_published_when_no_inbox_work_queue_uri_is_specified()
    {
        var schedule = new Schedule(Guid.NewGuid(), "schedule-name", EveryMinuteCronExpression, null, null);

        Assert.That(schedule.ShouldPublish, Is.True);
    }

    [Test]
    public void Should_indicate_that_a_notification_should_not_be_published_when_an_inbox_work_queue_uri_is_specified()
    {
        var schedule = new Schedule(Guid.NewGuid(), "schedule-name", EveryMinuteCronExpression, null, "queue://work");

        Assert.That(schedule.ShouldPublish, Is.False);
    }

    [Test]
    public void Should_be_able_to_set_the_next_notification()
    {
        var nextNotification = DateTime.UtcNow.AddDays(1);

        var schedule = new Schedule(Guid.NewGuid(), "schedule-name", EveryMinuteCronExpression, nextNotification, null);

        schedule.SetNextNotification();

        Assert.That(schedule.NextNotification, Is.Not.EqualTo(nextNotification));
        Assert.That(schedule.NextNotification, Is.GreaterThan(DateTime.UtcNow));
    }

    [Test]
    public void Should_be_able_to_set_the_name()
    {
        var schedule = new Schedule(Guid.NewGuid(), "schedule-name", EveryMinuteCronExpression, null, null);

        schedule.SetName("new-name");

        Assert.That(schedule.Name, Is.EqualTo("new-name"));
    }

    [Test]
    public void Should_throw_when_setting_an_empty_name()
    {
        var schedule = new Schedule(Guid.NewGuid(), "schedule-name", EveryMinuteCronExpression, null, null);

        Assert.That(() => schedule.SetName(string.Empty), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void Should_be_able_to_set_the_cron_expression_and_recalculate_the_next_notification()
    {
        var schedule = new Schedule(Guid.NewGuid(), "schedule-name", EveryMinuteCronExpression, DateTime.UtcNow.AddDays(1), null);

        schedule.SetCronExpression(EveryMinuteCronExpression);

        Assert.That(schedule.CronExpression, Is.EqualTo(EveryMinuteCronExpression));
        Assert.That(schedule.NextNotification, Is.LessThan(DateTime.UtcNow.AddDays(1)));
    }

    [Test]
    public void Should_throw_when_setting_an_empty_cron_expression()
    {
        var schedule = new Schedule(Guid.NewGuid(), "schedule-name", EveryMinuteCronExpression, null, null);

        Assert.That(() => schedule.SetCronExpression(string.Empty), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void Should_be_able_to_set_the_inbox_work_queue_uri()
    {
        var schedule = new Schedule(Guid.NewGuid(), "schedule-name", EveryMinuteCronExpression, null, null);

        schedule.SetInboxWorkQueueUri("queue://work");

        Assert.That(schedule.InboxWorkQueueUri, Is.EqualTo("queue://work"));
        Assert.That(schedule.ShouldPublish, Is.False);
    }

    [Test]
    public void Should_be_able_to_clear_the_inbox_work_queue_uri()
    {
        var schedule = new Schedule(Guid.NewGuid(), "schedule-name", EveryMinuteCronExpression, null, "queue://work");

        schedule.SetInboxWorkQueueUri(null);

        Assert.That(schedule.InboxWorkQueueUri, Is.Null);
        Assert.That(schedule.ShouldPublish, Is.True);
    }

    [Test]
    public void Should_be_able_to_build_a_specification()
    {
        var id = Guid.NewGuid();
        var fromInclusive = DateTime.UtcNow;
        var toExclusive = DateTime.UtcNow.AddDays(1);

        var specification = new Schedule.Specification()
            .AddId(id)
            .WithNameMatch("schedule")
            .WithNextNotificationFromInclusive(fromInclusive)
            .WithNextNotificationToExclusive(toExclusive);

        Assert.That(specification.Ids, Does.Contain(id));
        Assert.That(specification.NameMatch, Is.EqualTo("schedule"));
        Assert.That(specification.NextNotificationFromInclusive, Is.EqualTo(fromInclusive));
        Assert.That(specification.NextNotificationToExclusive, Is.EqualTo(toExclusive));
    }
}
