using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shuttle.Hopper.AzureStorageQueues;
using Shuttle.Hopper.Scheduling.SqlServer;

namespace Shuttle.Hopper.Scheduling.Server;

internal class Program
{
    private static async Task Main()
    {
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

        var configurationFolder = Environment.GetEnvironmentVariable("CONFIGURATION_FOLDER");

        if (string.IsNullOrEmpty(configurationFolder))
        {
            throw new ApplicationException("Environment variable `CONFIGURATION_FOLDER` has not been set.");
        }

        var appsettingsPath = Path.Combine(configurationFolder, "appsettings.json");

        if (!File.Exists(appsettingsPath))
        {
            throw new ApplicationException($"File '{appsettingsPath}' cannot be accessed/found.");
        }

        await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                var configuration = new ConfigurationBuilder()
                    .AddUserSecrets<Program>()
                    .AddJsonFile(appsettingsPath)
                    .Build();

                services
                    .AddSingleton<IConfiguration>(configuration)
                    .AddLogging(loggingBuilder =>
                    {
                        loggingBuilder.AddConsole();
                    })
                    .AddScheduling()
                    .UseSqlServer(options =>
                    {
                        configuration.GetSection(SchedulingSqlServerOptions.SectionName).Bind(options);

                        options.ConnectionString = configuration.GetConnectionString("Automation") ?? throw new ApplicationException("Missing connection string 'Automation'.");
                    })
                    .Services
                    .AddHopper(options =>
                    {
                        configuration.GetSection(HopperOptions.SectionName).Bind(options);
                    })
                    .AddMessageHandlersFrom(typeof(Program).Assembly)
                    .UseAzureStorageQueues(builder =>
                    {
                        builder.Configure("azure", options =>
                        {
                            configuration.GetSection($"{AzureStorageQueueOptions.SectionName}").Bind(options);

                            if (string.IsNullOrWhiteSpace(options.StorageAccount))
                            {
                                options.ConnectionString = configuration.GetConnectionString("azure") ?? throw new ApplicationException("Missing connection string 'azure'.");
                            }
                        });
                    })
                    .Services
                    .AddHostedService<Scheduler>();
            })
            .Build()
            .RunAsync();
    }
}