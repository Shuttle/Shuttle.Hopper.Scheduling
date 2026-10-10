using Microsoft.Extensions.Options;

namespace Shuttle.Hopper.Scheduling.SqlServer;

public class SchedulingSqlServerOptionsValidator : IValidateOptions<SchedulingSqlServerOptions>
{
    public ValidateOptionsResult Validate(string? name, SchedulingSqlServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail("Options 'ConnectionString' is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Schema))
        {
            return ValidateOptionsResult.Fail("Options 'Schema' is required.");
        }

        return ValidateOptionsResult.Success;
    }
}