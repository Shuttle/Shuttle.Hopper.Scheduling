namespace Shuttle.Hopper.Scheduling.SqlServer;

public class SchedulingSqlServerOptions
{
    public const string SectionName = "Shuttle:Hopper:Scheduling:SqlServer";
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromMinutes(1);

    public string ConnectionString { get; set; } = "Automation";
    public string Schema { get; set; } = "scheduling";
}