using Microsoft.Extensions.DependencyInjection;

namespace Shuttle.Hopper.Scheduling;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public SchedulingBuilder AddScheduling()
        {
            ArgumentNullException.ThrowIfNull(services);

            return new(services);
        }
    }
}