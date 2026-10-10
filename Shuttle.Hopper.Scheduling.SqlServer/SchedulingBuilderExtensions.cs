using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Shuttle.Hopper.Scheduling.SqlServer;

public static class SchedulingBuilderExtensions
{
    extension(SchedulingBuilder schedulingBuilder)
    {
        public SchedulingBuilder UseSqlServer(Action<SchedulingSqlServerOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(schedulingBuilder);

            schedulingBuilder.Services.AddOptions<SchedulingSqlServerOptions>().Configure(configureOptions);
            schedulingBuilder.Services.AddSingleton<IValidateOptions<SchedulingSqlServerOptions>, SchedulingSqlServerOptionsValidator>();

            schedulingBuilder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();

            schedulingBuilder.Services.AddDbContext<SchedulingDbContext>((serviceProvider, options) =>
            {
                var schedulingDbContextOptions = serviceProvider.GetRequiredService<IOptions<SchedulingSqlServerOptions>>();

                options.UseSqlServer(schedulingDbContextOptions.Value.ConnectionString, sqlServerOptions =>
                {
                    sqlServerOptions.CommandTimeout((int)schedulingDbContextOptions.Value.CommandTimeout.TotalSeconds);
                });
            });

            return schedulingBuilder;
        }
    }
}