using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Mvc;
using Shuttle.Access.AspNetCore;

namespace Shuttle.Hopper.Scheduling.WebApi;

public static class ScheduleEndpoints
{
    public static WebApplication MapScheduleEndpoints(this WebApplication app, ApiVersionSet versionSet)
    {
        var apiVersion1 = new ApiVersion(1, 0);

        app.MapPost("/v{version:apiVersion}/schedules/search", PostSearch)
            .WithTags("Schedules")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(apiVersion1)
            .RequireSession();

        app.MapGet("/v{version:apiVersion}/schedules/{id:Guid}", Get)
            .WithTags("Schedules")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(apiVersion1)
            .RequireSession();

        app.MapPost("/v{version:apiVersion}/schedules", Post)
            .WithTags("Schedules")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(apiVersion1)
            .RequireSession();

        app.MapDelete("/v{version:apiVersion}/schedules/{id:Guid}", Delete)
            .WithTags("Schedules")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(apiVersion1)
            .RequireSession();

        app.MapPatch("/v{version:apiVersion}/schedules/{id:Guid}/name", PatchName)
            .WithTags("Schedules")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(apiVersion1)
            .RequireSession();

        app.MapPatch("/v{version:apiVersion}/schedules/{id:Guid}/cron-expression", PatchCronExpression)
            .WithTags("Schedules")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(apiVersion1)
            .RequireSession();

        app.MapPatch("/v{version:apiVersion}/schedules/{id:Guid}/inbox-work-queue-uri", PatchInboxWorkQueueUri)
            .WithTags("Schedules")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(apiVersion1)
            .RequireSession();

        return app;
    }

    private static async Task<IResult> Delete(Guid id, [FromServices] IScheduleRepository scheduleRepository, CancellationToken cancellationToken)
    {
        await scheduleRepository.RemoveAsync(id, cancellationToken);

        return Results.Accepted();
    }

    private static async Task<IResult> Get(Guid id, [FromServices] IScheduleRepository scheduleRepository, CancellationToken cancellationToken)
    {
        var schedule = await scheduleRepository.FindAsync(id, cancellationToken);

        return schedule != null
            ? Results.Ok(Map(schedule))
            : Results.NotFound();
    }

    private static Contracts.v1.Schedule Map(Schedule schedule)
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

    private static async Task<IResult> PatchCronExpression(Guid id, [FromBody] Contracts.v1.SetCronExpression message, [FromServices] IScheduleRepository scheduleRepository, CancellationToken cancellationToken)
    {
        var schedule = await scheduleRepository.FindAsync(id, cancellationToken);

        if (schedule == null)
        {
            return Results.NotFound();
        }

        schedule.SetCronExpression(message.CronExpression);

        await scheduleRepository.SaveAsync(schedule, cancellationToken);

        return Results.Accepted();
    }

    private static async Task<IResult> PatchInboxWorkQueueUri(Guid id, [FromBody] Contracts.v1.SetInboxWorkQueueUri message, [FromServices] IScheduleRepository scheduleRepository, CancellationToken cancellationToken)
    {
        var schedule = await scheduleRepository.FindAsync(id, cancellationToken);

        if (schedule == null)
        {
            return Results.NotFound();
        }

        schedule.SetInboxWorkQueueUri(message.InboxWorkQueueUri);

        await scheduleRepository.SaveAsync(schedule, cancellationToken);

        return Results.Accepted();
    }

    private static async Task<IResult> PatchName(Guid id, [FromBody] Contracts.v1.SetName message, [FromServices] IScheduleRepository scheduleRepository, CancellationToken cancellationToken)
    {
        var schedule = await scheduleRepository.FindAsync(id, cancellationToken);

        if (schedule == null)
        {
            return Results.NotFound();
        }

        schedule.SetName(message.Name);

        await scheduleRepository.SaveAsync(schedule, cancellationToken);

        return Results.Accepted();
    }

    private static async Task<IResult> Post([FromBody] Contracts.v1.RegisterSchedule message, [FromServices] IScheduleRepository scheduleRepository, CancellationToken cancellationToken)
    {
        var schedule = new Schedule(message.Id ?? Guid.NewGuid(), message.Name, message.CronExpression, null, message.InboxWorkQueueUri);

        await scheduleRepository.AddAsync(schedule, cancellationToken);

        return Results.Ok(Map(schedule));
    }

    private static async Task<IResult> PostSearch([FromServices] IScheduleRepository scheduleRepository, [FromBody] Contracts.v1.Schedule.Specification specification, CancellationToken cancellationToken)
    {
        var search = new Schedule.Specification().AddIds(specification.Ids);

        if (!string.IsNullOrWhiteSpace(specification.NameMatch))
        {
            search.WithNameMatch(specification.NameMatch);
        }

        if (specification.NextNotificationFromInclusive.HasValue)
        {
            search.WithNextNotificationFromInclusive(specification.NextNotificationFromInclusive.Value);
        }

        if (specification.NextNotificationToExclusive.HasValue)
        {
            search.WithNextNotificationToExclusive(specification.NextNotificationToExclusive.Value);
        }

        return Results.Ok((await scheduleRepository.SearchAsync(search, cancellationToken)).Select(Map).ToList());
    }
}
