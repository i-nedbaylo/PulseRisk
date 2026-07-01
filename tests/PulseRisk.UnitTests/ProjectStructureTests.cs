using FluentAssertions;
using PulseRisk.Domain;

namespace PulseRisk.UnitTests;

public sealed class ProjectStructureTests
{
    [Fact]
    public void DomainAssembly_ShouldBeLoadable()
    {
        typeof(DomainAssemblyMarker).Assembly.GetName().Name
            .Should()
            .Be("PulseRisk.Domain");
    }
}

