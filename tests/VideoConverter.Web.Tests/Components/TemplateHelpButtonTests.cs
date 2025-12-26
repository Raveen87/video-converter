using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using VideoConverter.Web.Components;
using Xunit;
using static VideoConverter.Web.Components.TemplateHelpDialog;

namespace VideoConverter.Web.Tests.Components;

/// <summary>
/// Tests for the TemplateHelpButton component.
/// </summary>
public sealed class TemplateHelpButtonTests : BunitContext
{
    private readonly Mock<IDialogService> _dialogServiceMock;

    public TemplateHelpButtonTests()
    {
        _dialogServiceMock = new Mock<IDialogService>();
        Services.AddSingleton(_dialogServiceMock.Object);
        Services.AddMudServices();

        // Stub MudTooltip to avoid MudPopoverProvider requirement
        ComponentFactories.AddStub<MudTooltip>(parameters =>
            builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "mud-tooltip-stub");
                parameters.Get(x => x.ChildContent)?.Invoke(builder);
                builder.CloseElement();
            });
    }

    [Fact]
    public void Render_DisplaysIconButtonWithCorrectIcon()
    {
        // Arrange & Act
        var cut = Render<TemplateHelpButton>();

        // Assert
        var button = cut.Find("button");
        button.Should().NotBeNull();

        // Check that the help icon is present
        var svg = cut.Find("svg");
        svg.Should().NotBeNull();
    }

    [Fact]
    public void Render_WrapsButtonInTooltip()
    {
        // Arrange & Act
        var cut = Render<TemplateHelpButton>();

        // Assert
        // MudTooltip is stubbed, so we check for the stub wrapper
        cut.Markup.Should().Contain("mud-tooltip-stub");

        // Button should still be present within the stub
        var button = cut.Find("button");
        button.Should().NotBeNull();
    }

    [Fact]
    public void OnClick_OpensHelpDialog_WithFilenameTemplateType()
    {
        // Arrange
        var dialogReferenceMock = new Mock<IDialogReference>();

        _dialogServiceMock.Setup(x => x.ShowAsync<TemplateHelpDialog>(
                It.IsAny<string>(),
                It.IsAny<DialogParameters<TemplateHelpDialog>>(),
                It.IsAny<DialogOptions>()))
            .ReturnsAsync(dialogReferenceMock.Object);

        var cut = Render<TemplateHelpButton>(parameters => parameters
            .Add(p => p.TemplateType, TemplateType.Filename));

        // Act
        var button = cut.Find("button");
        button.Click();

        // Assert
        _dialogServiceMock.Verify(
            x => x.ShowAsync<TemplateHelpDialog>(
            "Template Guide",
            It.IsAny<DialogParameters<TemplateHelpDialog>>(),
            It.Is<DialogOptions>(o =>
                o.CloseOnEscapeKey == true &&
                o.MaxWidth == MaxWidth.Medium &&
                o.FullWidth == true)),
            Times.Once);
    }

    [Fact]
    public void OnClick_OpensHelpDialog_WithPathTemplateType()
    {
        // Arrange
        var dialogReferenceMock = new Mock<IDialogReference>();

        _dialogServiceMock.Setup(x => x.ShowAsync<TemplateHelpDialog>(
                It.IsAny<string>(),
                It.IsAny<DialogParameters<TemplateHelpDialog>>(),
                It.IsAny<DialogOptions>()))
            .ReturnsAsync(dialogReferenceMock.Object);

        var cut = Render<TemplateHelpButton>(parameters => parameters
            .Add(p => p.TemplateType, TemplateType.Path));

        // Act
        var button = cut.Find("button");
        button.Click();

        // Assert
        _dialogServiceMock.Verify(
            x => x.ShowAsync<TemplateHelpDialog>(
            "Template Guide",
            It.IsAny<DialogParameters<TemplateHelpDialog>>(),
            It.Is<DialogOptions>(o =>
                o.CloseOnEscapeKey == true &&
                o.MaxWidth == MaxWidth.Medium &&
                o.FullWidth == true)),
            Times.Once);
    }

    [Fact]
    public void Render_AppliesCustomCssClass_WhenProvided()
    {
        // Arrange
        const string customClass = "my-custom-class";

        // Act
        var cut = Render<TemplateHelpButton>(parameters => parameters
            .Add(p => p.Class, customClass));

        // Assert
        var button = cut.Find("button");
        button.ClassList.Should().Contain(customClass);
    }

    [Fact]
    public void OnClick_WithDefaultTemplateType_OpensDialogWithCorrectTitle()
    {
        // Arrange
        var dialogReferenceMock = new Mock<IDialogReference>();
        _dialogServiceMock.Setup(x => x.ShowAsync<TemplateHelpDialog>(
                It.IsAny<string>(),
                It.IsAny<DialogParameters<TemplateHelpDialog>>(),
                It.IsAny<DialogOptions>()))
            .ReturnsAsync(dialogReferenceMock.Object);

        // Act - Render without specifying TemplateType (defaults to Filename)
        var cut = Render<TemplateHelpButton>();
        var button = cut.Find("button");
        button.Click();

        // Assert - Should call ShowAsync with correct title and default TemplateType
        _dialogServiceMock.Verify(
            x => x.ShowAsync<TemplateHelpDialog>(
            "Template Guide",
            It.IsAny<DialogParameters<TemplateHelpDialog>>(),
            It.IsAny<DialogOptions>()),
            Times.Once);
    }
}
