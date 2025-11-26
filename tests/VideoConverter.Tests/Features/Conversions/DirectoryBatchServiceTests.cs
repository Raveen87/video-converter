using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Api.Shared.Validation;
using VideoConverter.Common.Models;
using VideoConverter.Tests.TestUtils;

namespace VideoConverter.Tests.Features.Conversions;

public class DirectoryBatchServiceTests
{
    [Fact]
    public async Task CreateBatchAsync_CreatesBatchAndItems_ForFilesInDirectory()
    {
        using var tempRoot = new TempDir();
        var inputDir = tempRoot.CreateSubdir("in");
        var outputDir = tempRoot.CreateSubdir("out");
        var mkv = Path.Combine(inputDir, "a.mkv");
        var mp4 = Path.Combine(inputDir, "b.mp4");
        await File.WriteAllTextAsync(mkv, "x");
        await File.WriteAllTextAsync(mp4, "x");

        await using var db = DbContextTestFactory.CreateInMemory();
        var fileValidator = new FileSystemValidator();
        var svc = new DirectoryBatchService(db, fileValidator, NullLogger<DirectoryBatchService>.Instance);

        var dto = new DirectoryQueueRequestDto(
            DirectoryPath: inputDir,
            OutputDirectory: outputDir,
            Crf: 23,
            AudioBitrateKbps: 128,
            AudioChannels: null,
            Resolution: 1080,
            AudioLanguages: null,
            SubtitleLanguages: null,
            OutputFilenameTemplate: null,
            Extensions: null);

        var batchId = await svc.CreateBatchAsync(dto, profileId: null, CancellationToken.None);

        batchId.Should().BeGreaterThan(0);
        var batch = await db.DirectoryQueueBatches.Include(b => b.Items).FirstAsync();
        batch.TotalItems.Should().Be(2);
        batch.Items.Count.Should().Be(2);
        batch.Status.Should().Be("Pending");
        batch.ProcessedItems.Should().Be(0);
    }
}
