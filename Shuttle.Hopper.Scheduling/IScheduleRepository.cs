namespace Shuttle.Hopper.Scheduling;

public interface IScheduleRepository
{
    Task AddAsync(Schedule schedule, CancellationToken cancellationToken = default);
    Task<IEnumerable<Schedule>> AllAsync(CancellationToken cancellationToken = default);
    Task<Schedule?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveAsync(Schedule schedule, CancellationToken cancellationToken = default);
    Task<IEnumerable<Schedule>> SearchAsync(Schedule.Specification specification, CancellationToken cancellationToken = default);
    Task SetNextNotificationAsync(Guid id, DateTime nextNotification, CancellationToken cancellationToken = default);
}