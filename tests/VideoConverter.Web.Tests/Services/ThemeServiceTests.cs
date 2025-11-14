using FluentAssertions;
using Moq;
using VideoConverter.Web.Services;
using Xunit;

namespace VideoConverter.Web.Tests.Services;

public class ThemeServiceTests
{
    private readonly Mock<IJSInteropWrapper> _jsInteropMock;
    private readonly ThemeService _themeService;

    public ThemeServiceTests()
    {
        _jsInteropMock = new Mock<IJSInteropWrapper>();
        _themeService = new ThemeService(_jsInteropMock.Object);
    }

    [Fact]
    public async Task IsDarkModeAsync_WhenNoStoredPreference_ReturnsSystemPreference()
    {
        // Arrange
        _jsInteropMock
            .Setup(x => x.InvokeAsync<bool>(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _themeService.IsDarkModeAsync();

        // Assert
        result.Should().BeTrue();
        _jsInteropMock.Verify(x => x.InvokeAsync<bool>(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsDarkModeAsync_WhenCalledMultipleTimes_OnlyInitializesOnce()
    {
        // Arrange
        _jsInteropMock
            .Setup(x => x.InvokeAsync<bool>(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result1 = await _themeService.IsDarkModeAsync();
        var result2 = await _themeService.IsDarkModeAsync();
        var result3 = await _themeService.IsDarkModeAsync();

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeFalse();
        result3.Should().BeFalse();
        _jsInteropMock.Verify(x => x.InvokeAsync<bool>(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetDarkModeAsync_WhenThemeChanges_InvokesJavaScriptAndRaisesEvent()
    {
        // Arrange
        _jsInteropMock
            .Setup(x => x.InvokeAsync<bool>(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _jsInteropMock
            .Setup(x => x.InvokeVoidAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        bool? eventRaised = null;
        _themeService.ThemeChanged += (sender, e) => eventRaised = e.IsDark;

        // Act
        await _themeService.SetDarkModeAsync(true);

        // Assert
        eventRaised.Should().BeTrue();
        _jsInteropMock.Verify(x => x.InvokeVoidAsync(It.IsAny<string>(), It.Is<object?[]>(args => args.Length > 0 && args[0] is bool && (bool)args[0]!), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetDarkModeAsync_WhenThemeDoesNotChange_DoesNotInvokeJavaScriptOrRaiseEvent()
    {
        // Arrange
        _jsInteropMock
            .Setup(x => x.InvokeAsync<bool>(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        bool eventRaised = false;
        _themeService.ThemeChanged += (sender, e) => eventRaised = true;

        // Act
        await _themeService.SetDarkModeAsync(false);

        // Assert
        eventRaised.Should().BeFalse();
        _jsInteropMock.Verify(x => x.InvokeVoidAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ToggleThemeAsync_WhenCalled_TogglesTheme()
    {
        // Arrange
        _jsInteropMock
            .Setup(x => x.InvokeAsync<bool>(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _jsInteropMock
            .Setup(x => x.InvokeVoidAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _themeService.ToggleThemeAsync();

        // Assert
        var result = await _themeService.IsDarkModeAsync();
        result.Should().BeTrue();
        _jsInteropMock.Verify(x => x.InvokeVoidAsync(It.IsAny<string>(), It.Is<object?[]>(args => args.Length > 0 && args[0] is bool && (bool)args[0]!), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsDarkModeAsync_WhenJavaScriptThrows_ReturnsFalse()
    {
        // Arrange
        _jsInteropMock
            .Setup(x => x.InvokeAsync<bool>(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Error"));

        // Act
        var result = await _themeService.IsDarkModeAsync();

        // Assert
        result.Should().BeFalse();
    }
}
