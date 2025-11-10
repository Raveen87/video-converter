using System.Diagnostics;
using VideoConverter.Api.Features.Integrations.Ffmpeg;

namespace VideoConverter.Tests.Services.Conversion;

public class ProcessRegistryTests
{
    [Fact]
    public void Register_And_TryGet_ShouldSucceed()
    {
        // Arrange
        var registry = new RunningProcessRegistry();
        using var process = new Process();

        // Act
        registry.Register("conversion:123", process);
        var found = registry.TryGet("conversion:123", out var handle);

        // Assert
        Assert.True(found);
        Assert.Equal(process, handle.Process);
    }

    [Fact]
    public void Unregister_ShouldRemove()
    {
        // Arrange
        var registry = new RunningProcessRegistry();
        using var process = new Process();
        registry.Register("conversion:abc", process);

        // Act
        registry.Unregister("conversion:abc");
        var found = registry.TryGet("conversion:abc", out _);

        // Assert
        Assert.False(found);
    }
}
