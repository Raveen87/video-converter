using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using VideoConverter.Common.Models;
using VideoConverter.Web.Components;
using VideoConverter.Web.Services;
using Xunit;

namespace VideoConverter.Web.Tests.Components;

public sealed class FileBrowserTests : BunitContext
{
    private readonly Mock<IFileBrowserService> _fileBrowserServiceMock;

    public FileBrowserTests()
    {
        _fileBrowserServiceMock = new Mock<IFileBrowserService>();
        Services.AddSingleton(_fileBrowserServiceMock.Object);
        Services.AddLogging();
        Services.AddMudServices();
    }

    [Fact]
    public void Breadcrumbs_RemainVisible_DuringNavigation()
    {
        // Arrange
        var initialResponse = new FileBrowserResponseDto(
            "/",
            null,
            new List<DirectoryItemDto>(),
            new List<FileItemDto>());

        // Initial load with null path (defaults to root)
        _fileBrowserServiceMock.Setup(x => x.GetDirectoryContentsAsync(null))
            .ReturnsAsync(initialResponse);

        var tcs = new TaskCompletionSource<FileBrowserResponseDto>();

        // Replace MudDialog with a stub that renders its DialogContent
        ComponentFactories.AddStub<MudDialog>(parameters =>
            builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "mud-dialog-stub");
                parameters.Get(x => x.DialogContent)?.Invoke(builder);
                builder.CloseElement();
            });

        var cut = Render<FileBrowser>(parameters => parameters
            .Add(p => p.InitialPath, null));

        // Verify loaded state
        try
        {
            cut.WaitForState(() => cut.FindComponents<MudBreadcrumbs>().Count > 0, TimeSpan.FromSeconds(1));
        }
        catch (Exception)
        {
            Assert.Fail($"Breadcrumbs not found. Markup: {cut.Markup}");
        }

        // Reconfigure mock to hang on next call (navigation)
        _fileBrowserServiceMock.Setup(x => x.GetDirectoryContentsAsync(It.IsAny<string>()))
            .Returns(tcs.Task);

        // Act - Navigate using first available breadcrumb
        // Note: MudLink without Href still renders as an anchor but without href attribute.
        // Bunit FindAll("a") finds it.
        var links = cut.FindAll("a.mud-link");
        var linkToClick = links.FirstOrDefault();

        Assert.NotNull(linkToClick);
        Assert.False(linkToClick.HasAttribute("href"), "MudLink should not have href attribute to avoid navigation");

        linkToClick.Click();

        // Assert
        // 1. Progress bar should be visible
        Assert.NotEmpty(cut.FindComponents<MudProgressLinear>());

        // 2. Breadcrumbs should still be visible (this prevents the focus trap error)
        Assert.NotEmpty(cut.FindComponents<MudBreadcrumbs>());
    }
}
