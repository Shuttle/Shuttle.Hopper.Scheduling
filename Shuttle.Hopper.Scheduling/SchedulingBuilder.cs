using Microsoft.Extensions.DependencyInjection;

namespace Shuttle.Hopper.Scheduling;

public class SchedulingBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services;
}