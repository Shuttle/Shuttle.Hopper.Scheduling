using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Shuttle.Hopper.Scheduling.SqlServer.Models;

[Index(nameof(Name), nameof(CronExpression), nameof(InboxWorkQueueUri), Name = $"IX_{nameof(Schedule)}", IsUnique = true)]
public class Schedule
{
    [StringLength(130)]
    public string CronExpression { get; set; } = string.Empty;

    [Key]
    public Guid Id { get; set; }

    [StringLength(130)]
    public string? InboxWorkQueueUri { get; set; }

    [StringLength(130)]
    public string Name { get; set; } = string.Empty;

    public DateTime NextNotification { get; set; }
}