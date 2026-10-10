using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Shuttle.Hopper.Scheduling.SqlServer;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SchedulingDbContext>
{
    public SchedulingDbContext CreateDbContext(string[] args)
    {
        /*
            Right-click on `Shuttle.Hopper.Scheduling.SqlServer` and select `Manage User Secrets`
            {
              "Shuttle": {
                "Hopper": {
                  "Scheduling": {
                    "SqlServer: {
                      "ConnectionString": "Data Source=.;Initial Catalog=Scheduling;user id=<user>;password=<password>;TrustServerCertificate=true"
                    }
                  }
                }
              }
            }
        */

        var optionsBuilder = new DbContextOptionsBuilder<SchedulingDbContext>();

        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<DesignTimeDbContextFactory>()
            .Build();

        var schedulingDbContextOptions = configuration.GetSection(SchedulingSqlServerOptions.SectionName).Get<SchedulingSqlServerOptions>();

        if (schedulingDbContextOptions == null)
        {
            throw new ArgumentException($"Could not find a configuration section for '{SchedulingSqlServerOptions.SectionName}'.");
        }

        optionsBuilder.UseSqlServer(schedulingDbContextOptions.ConnectionString, builder => builder.MigrationsHistoryTable("__EFMigrationsHistory", schedulingDbContextOptions.Schema));

        return new(optionsBuilder.Options, Options.Create(schedulingDbContextOptions));
    }
}