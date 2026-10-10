using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Shuttle.Hopper.Scheduling.Tests;

[TestFixture]
public class SchedulingBuilderFixture
{
    [Test]
    public void Should_expose_the_given_service_collection()
    {
        var services = new ServiceCollection();

        var builder = new SchedulingBuilder(services);

        Assert.That(builder.Services, Is.SameAs(services));
    }
}
