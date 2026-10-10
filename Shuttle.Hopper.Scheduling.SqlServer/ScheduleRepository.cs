using Microsoft.EntityFrameworkCore;

namespace Shuttle.Hopper.Scheduling.SqlServer;

public class ScheduleRepository(SchedulingDbContext dbContext) : IScheduleRepository
{
    public async Task AddAsync(Schedule schedule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        await dbContext.Schedules.AddAsync(Map(schedule), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<Schedule>> AllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return (await dbContext.Schedules.AsNoTracking().ToListAsync(cancellationToken)).Select(Map);
        }
        catch (OperationCanceledException)
        {
            return [];
        }
    }

    public async Task<Schedule?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await dbContext.Schedules.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return model == null ? null : Map(model);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await dbContext.Schedules.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (model == null)
        {
            return;
        }

        dbContext.Schedules.Remove(model);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveAsync(Schedule schedule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        var model = await dbContext.Schedules.FirstOrDefaultAsync(item => item.Id == schedule.Id, cancellationToken);

        if (model == null)
        {
            return;
        }

        model.Name = schedule.Name;
        model.CronExpression = schedule.CronExpression;
        model.InboxWorkQueueUri = schedule.InboxWorkQueueUri;
        model.NextNotification = schedule.NextNotification;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<Schedule>> SearchAsync(Schedule.Specification specification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var query = dbContext.Schedules.AsNoTracking().AsQueryable();

        if (specification.Ids.Count > 0)
        {
            query = query.Where(item => specification.Ids.Contains(item.Id));
        }

        if (!string.IsNullOrWhiteSpace(specification.NameMatch))
        {
            query = query.Where(item => item.Name.Contains(specification.NameMatch));
        }

        if (specification.NextNotificationFromInclusive.HasValue)
        {
            query = query.Where(item => item.NextNotification >= specification.NextNotificationFromInclusive.Value);
        }

        if (specification.NextNotificationToExclusive.HasValue)
        {
            query = query.Where(item => item.NextNotification < specification.NextNotificationToExclusive.Value);
        }

        return (await query.ToListAsync(cancellationToken)).Select(Map);
    }

    public async Task SetNextNotificationAsync(Guid id, DateTime nextNotification, CancellationToken cancellationToken = default)
    {
        var model = await dbContext.Schedules.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (model == null)
        {
            return;
        }

        model.NextNotification = nextNotification;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Schedule Map(Models.Schedule model)
    {
        return new(model.Id, model.Name, model.CronExpression, model.NextNotification, model.InboxWorkQueueUri);
    }

    private static Models.Schedule Map(Schedule schedule)
    {
        return new()
        {
            Id = schedule.Id,
            Name = schedule.Name,
            CronExpression = schedule.CronExpression,
            NextNotification = schedule.NextNotification,
            InboxWorkQueueUri = schedule.InboxWorkQueueUri
        };
    }
}
