using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Shuttle.Hopper.Scheduling.SqlServer;

public class SchedulingDbContext : DbContext
{
    private readonly SchedulingSqlServerOptions _schedulingSqlServerOptions;

    public SchedulingDbContext(DbContextOptions<SchedulingDbContext> options, IOptions<SchedulingSqlServerOptions> schedulingDbContextOptions) : base(options)
    {
        ArgumentNullException.ThrowIfNull(schedulingDbContextOptions);

        _schedulingSqlServerOptions = schedulingDbContextOptions.Value;
    }

    public DbSet<Models.Schedule> Schedules { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(_schedulingSqlServerOptions.Schema);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            entityType.SetTableName(entityType.DisplayName());
        }
    }
}