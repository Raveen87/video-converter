using Bunit;
using MudBlazor.Services;
using VideoConverter.Web.Components;

namespace VideoConverter.Web.Tests.Components;

public sealed class PathWithTemplateInputTests : BunitContext
{
    public PathWithTemplateInputTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void TextInput_WithDirectoryPathTemplateAndFilename_MapsToSplitFields()
    {
        // Arrange
        string? directory = null;
        string? pathTemplate = null;
        string? filenameTemplate = null;

        var cut = Render<PathWithTemplateInput>(parameters => parameters
            .Add(p => p.Directory, directory)
            .Add(p => p.DirectoryChanged, value => directory = value)
            .Add(p => p.PathTemplate, pathTemplate)
            .Add(p => p.PathTemplateChanged, value => pathTemplate = value)
            .Add(p => p.FilenameTemplate, filenameTemplate)
            .Add(p => p.FilenameTemplateChanged, value => filenameTemplate = value));

        // Act
        var input = cut.Find("input");
        input.Input("/out/{Type}/{Title}/{Title}.{Year}.mkv");

        // Assert
        directory.Should().Be("/out");
        pathTemplate.Should().Be("{Type}/{Title}");
        filenameTemplate.Should().Be("{Title}.{Year}.mkv");
    }

    [Fact]
    public void Parameters_WithSplitFields_ReconstructCombinedText()
    {
        // Arrange & Act
        var cut = Render<PathWithTemplateInput>(parameters => parameters
            .Add(p => p.Directory, "/out")
            .Add(p => p.PathTemplate, "{Type}/{Title}")
            .Add(p => p.FilenameTemplate, "{Title}.{Year}.mkv"));

        // Assert
        var input = cut.Find("input");
        input.GetAttribute("value").Should().Be("/out/{Type}/{Title}/{Title}.{Year}.mkv");
    }

    [Fact]
    public void TextInput_WithUnixDirectoryOnly_MapsToDirectoryOnly()
    {
        // Arrange
        string? directory = null;
        string? pathTemplate = null;
        string? filenameTemplate = null;

        var cut = Render<PathWithTemplateInput>(parameters => parameters
            .Add(p => p.Directory, directory)
            .Add(p => p.DirectoryChanged, value => directory = value)
            .Add(p => p.PathTemplate, pathTemplate)
            .Add(p => p.PathTemplateChanged, value => pathTemplate = value)
            .Add(p => p.FilenameTemplate, filenameTemplate)
            .Add(p => p.FilenameTemplateChanged, value => filenameTemplate = value));

        // Act
        var input = cut.Find("input");
        input.Input("/out");

        // Assert
        directory.Should().Be("/out");
        pathTemplate.Should().BeNull();
        filenameTemplate.Should().BeNull();
    }

    [Fact]
    public void TextInput_WithWindowsDirectoryOnly_MapsToDirectoryOnly()
    {
        // Arrange
        string? directory = null;
        string? pathTemplate = null;
        string? filenameTemplate = null;

        var cut = Render<PathWithTemplateInput>(parameters => parameters
            .Add(p => p.Directory, directory)
            .Add(p => p.DirectoryChanged, value => directory = value)
            .Add(p => p.PathTemplate, pathTemplate)
            .Add(p => p.PathTemplateChanged, value => pathTemplate = value)
            .Add(p => p.FilenameTemplate, filenameTemplate)
            .Add(p => p.FilenameTemplateChanged, value => filenameTemplate = value));

        // Act
        var input = cut.Find("input");
        input.Input("C:/out");

        // Assert
        directory.Should().Be("C:/out");
        pathTemplate.Should().BeNull();
        filenameTemplate.Should().BeNull();
    }

    [Fact]
    public void TextInput_WithUnixDirectoryOnlyTrailingSeparator_MapsToDirectoryOnly()
    {
        // Arrange
        string? directory = null;
        string? pathTemplate = null;
        string? filenameTemplate = null;

        var cut = Render<PathWithTemplateInput>(parameters => parameters
            .Add(p => p.Directory, directory)
            .Add(p => p.DirectoryChanged, value => directory = value)
            .Add(p => p.PathTemplate, pathTemplate)
            .Add(p => p.PathTemplateChanged, value => pathTemplate = value)
            .Add(p => p.FilenameTemplate, filenameTemplate)
            .Add(p => p.FilenameTemplateChanged, value => filenameTemplate = value));

        // Act
        var input = cut.Find("input");
        input.Input("/out/");

        // Assert
        directory.Should().Be("/out/");
        pathTemplate.Should().BeNull();
        filenameTemplate.Should().BeNull();
    }

    [Fact]
    public void TextInput_WithWindowsDirectoryOnlyTrailingSeparator_MapsToDirectoryOnly()
    {
        // Arrange
        string? directory = null;
        string? pathTemplate = null;
        string? filenameTemplate = null;

        var cut = Render<PathWithTemplateInput>(parameters => parameters
            .Add(p => p.Directory, directory)
            .Add(p => p.DirectoryChanged, value => directory = value)
            .Add(p => p.PathTemplate, pathTemplate)
            .Add(p => p.PathTemplateChanged, value => pathTemplate = value)
            .Add(p => p.FilenameTemplate, filenameTemplate)
            .Add(p => p.FilenameTemplateChanged, value => filenameTemplate = value));

        // Act
        var input = cut.Find("input");
        input.Input("C:/out/");

        // Assert
        directory.Should().Be("C:/out/");
        pathTemplate.Should().BeNull();
        filenameTemplate.Should().BeNull();
    }

    [Fact]
    public void TextInput_WithRootDirectoryOnly_MapsToDirectoryOnly()
    {
        // Arrange
        string? directory = null;
        string? pathTemplate = null;
        string? filenameTemplate = null;

        var cut = Render<PathWithTemplateInput>(parameters => parameters
            .Add(p => p.Directory, directory)
            .Add(p => p.DirectoryChanged, value => directory = value)
            .Add(p => p.PathTemplate, pathTemplate)
            .Add(p => p.PathTemplateChanged, value => pathTemplate = value)
            .Add(p => p.FilenameTemplate, filenameTemplate)
            .Add(p => p.FilenameTemplateChanged, value => filenameTemplate = value));

        // Act
        var input = cut.Find("input");
        input.Input("/");

        // Assert
        directory.Should().Be("/");
        pathTemplate.Should().BeNull();
        filenameTemplate.Should().BeNull();
    }
}
