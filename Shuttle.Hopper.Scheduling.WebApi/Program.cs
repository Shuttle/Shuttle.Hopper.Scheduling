using System.Data.Common;
using Asp.Versioning;
using Microsoft.Data.SqlClient;
using Scalar.AspNetCore;
using Serilog;
using Shuttle.Access.AspNetCore;
using Shuttle.Hopper.Scheduling.SqlServer;

namespace Shuttle.Hopper.Scheduling.WebApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", SqlClientFactory.Instance);

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

        var webApplicationBuilder = WebApplication.CreateBuilder(args);
        var configuration = webApplicationBuilder.Configuration;

        configuration
            .AddJsonFile(appsettingsPath)
            .AddUserSecrets<Program>(true)
            .AddEnvironmentVariables();

        var schedulingConnectionString = configuration.GetConnectionString("Automation") ?? throw new ApplicationException("Missing connection string 'Automation'.");

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        var apiVersion1 = new ApiVersion(1, 0);

        webApplicationBuilder.Services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = apiVersion1;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        webApplicationBuilder.Services
            .AddLogging(builder =>
            {
                builder.AddSerilog();
            })
            .AddEndpointsApiExplorer()
            .AddOpenApi(options =>
            {
                options.AddSchemaTransformer((schema, _, _) =>
                {
                    schema.Title = schema.Title?.Replace("+", "_");
                    return Task.CompletedTask;
                });
            })
            .AddAccessAuthorization(options =>
            {
                configuration.GetSection(AccessAuthorizationOptions.SectionName).Bind(options);
            })
            .Services
            .AddScheduling()
            .UseSqlServer(options =>
            {
                configuration.GetSection(SchedulingSqlServerOptions.SectionName).Bind(options);

                options.ConnectionString = schedulingConnectionString;
            })
            .Services
            .AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

        var app = webApplicationBuilder.Build();

        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(apiVersion1)
            .ReportApiVersions()
            .Build();

        app.UseCors("AllowAll");

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("Shuttle Hopper Scheduling API")
                .WithTheme(ScalarTheme.DeepSpace)
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });

        app.UseAccessAuthorization();

        app
            .MapScheduleEndpoints(versionSet)
            .MapServerEndpoints(versionSet);

        await app.RunAsync();
    }
}
