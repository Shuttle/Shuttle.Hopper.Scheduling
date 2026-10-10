using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shuttle.Contract;
using Shuttle.Hopper.Scheduling.Messages.v1;
using Shuttle.Reflection;

namespace Shuttle.Hopper.Scheduling.Server;

public class Scheduler : IHostedService
{
    private readonly CancellationToken _cancellationToken;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly TimeSpan _delayTimeSpan = TimeSpan.FromSeconds(5);
    private readonly ILogger<Scheduler> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private Task? _processor;

    public Scheduler(ILogger<Scheduler> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = Guard.AgainstNull(logger);
        _serviceScopeFactory = Guard.AgainstNull(serviceScopeFactory);
        _cancellationToken = _cancellationTokenSource.Token;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _processor = ProcessScheduleAsync();

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _cancellationTokenSource.CancelAsync();

        if (_processor != null)
        {
            await _processor;
        }
    }

    private async Task ProcessScheduleAsync()
    {
        try
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                var scope = _serviceScopeFactory.CreateScope();
                var scheduleRepository = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
                var bus = scope.ServiceProvider.GetRequiredService<IBus>();

                var schedules = scheduleRepository.AllAsync(_cancellationToken).GetAwaiter().GetResult();

                foreach (var schedule in schedules)
                {
                    if (!schedule.ShouldSendNotification)
                    {
                        continue;
                    }

                    try
                    {
                        if (schedule.ShouldPublish)
                        {
                            await bus.PublishAsync(new ScheduleNotification
                            {
                                Name = schedule.Name,
                                CronExpression = schedule.CronExpression,
                                NextNotification = schedule.NextNotification.ToUniversalTime()
                            }, cancellationToken: _cancellationToken);

                            _logger.LogDebug($"[published] : name = '{schedule.Name}' / cron = '{schedule.CronExpression}' / next notification = '{schedule.NextNotification:O}'");
                        }
                        else
                        {
                            await bus.SendAsync(new ScheduleNotification
                            {
                                Name = schedule.Name,
                                CronExpression = schedule.CronExpression,
                                NextNotification = schedule.NextNotification.ToUniversalTime()
                            }, builder => builder.WithRecipient(schedule.InboxWorkQueueUri!), _cancellationToken);

                            _logger.LogDebug($"[sent] : name = '{schedule.Name}' / cron = '{schedule.CronExpression}' / next notification = '{schedule.NextNotification:O}' / inbox work queue uri = '{schedule.InboxWorkQueueUri}'");
                        }

                        schedule.SetNextNotification();

                        scheduleRepository.SetNextNotificationAsync(schedule.Id, schedule.NextNotification.ToUniversalTime(), _cancellationToken).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.AllMessages());
                    }
                }

                Task.Delay(_delayTimeSpan, _cancellationToken).GetAwaiter().GetResult();
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.AllMessages());

            Environment.Exit(1);
        }
    }
}