using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Shuttle.Hopper.Scheduling.Tests;

[TestFixture]
public class ServiceCollectionExtensionsFixture
{
    [Test]
    public void Should_throw_when_services_is_null()
    {
        IServiceCollection? services = null;

        Assert.That(() => services!.AddScheduling(), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void Should_be_able_to_register_scheduling()
    {
        var services = new ServiceCollection();

        var builder = services.AddScheduling();

        Assert.That(builder, Is.Not.Null);
        Assert.That(builder.Services, Is.SameAs(services));
    }
}
