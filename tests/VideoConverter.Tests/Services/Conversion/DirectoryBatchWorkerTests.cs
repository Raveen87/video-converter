using System.Reflection;
using VideoConverter.Api.Services.Conversion;

namespace VideoConverter.Tests.Services.Conversion;

public sealed class DirectoryBatchWorkerTests
{
    [Fact]
    public void BuildPerFileRequest_PropagatesRequiredVmafAndVmafEnabled()
    {
        var request = new DirectoryQueueRequestDto(
            DirectoryPath: "/in",
            OutputDirectory: "/out",
            Crf: 32,
            AudioBitrateKbps: 160,
            AudioChannels: AudioChannels.Stereo20,
            Resolution: 1080,
            AudioLanguages: ["en"],
            SubtitleLanguages: ["sv"],
            OutputFilenameTemplate: null,
            OutputPathTemplate: null,
            Extensions: [".mkv"],
            VmafEnabled: true,
            RequiredVmaf: 93.4);

        var helpersType = typeof(DirectoryBatchWorker).GetNestedType("Helpers", BindingFlags.NonPublic);
        helpersType.Should().NotBeNull();
        var buildMethod = helpersType!.GetMethod("BuildPerFileRequest", BindingFlags.Public | BindingFlags.Static);
        buildMethod.Should().NotBeNull();

        var result = (ConversionCreateDto?)buildMethod!.Invoke(null, [request, "/in/a.mkv"]);

        result.Should().NotBeNull();
        result!.InputPath.Should().Be("/in/a.mkv");
        result.VmafEnabled.Should().BeTrue();
        result.RequiredVmaf.Should().Be(93.4);
    }
}
