using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Common.Models;
using VideoConverter.Tests.TestUtils;

namespace VideoConverter.Tests.Persistence;

public class ProfilePersistenceTests
{
    [Fact]
    public async Task AppDbContext_CanPersistProfileWithAllValues()
    {
        // Arrange
        await using var context = DbContextTestFactory.CreateInMemory();
        var profile = new Profile
        {
            OutputDirectory = "/videos/output",
            Crf = 20,
            AudioBitrateKbps = 192,
            AudioChannels = AudioChannels.Surround51,
            Resolution = 2160,
            MetadataBasedFilename = "Movie.Title.2160p.mkv",
            AudioLanguages = ["en", "sv"],
            SubtitleLanguages = ["sv"],
        };

        // Act
        _ = context.Profiles.Add(profile);
        _ = await context.SaveChangesAsync();

        // Assert
        var storedProfile = await context.Profiles.FirstAsync();
        _ = storedProfile.OutputDirectory.Should().Be("/videos/output");
        _ = storedProfile.Crf.Should().Be(20);
        _ = storedProfile.AudioBitrateKbps.Should().Be(192);
        _ = storedProfile.AudioChannels.Should().Be(AudioChannels.Surround51);
        _ = storedProfile.Resolution.Should().Be(2160);
        _ = storedProfile.MetadataBasedFilename.Should().Be("Movie.Title.2160p.mkv");
        _ = storedProfile.AudioLanguages.Should().BeEquivalentTo("en", "sv");
        _ = storedProfile.SubtitleLanguages.Should().BeEquivalentTo("sv");
    }

    [Fact]
    public async Task AppDbContext_CanPersistProfileWithNullValues()
    {
        // Arrange
        await using var context = DbContextTestFactory.CreateInMemory();
        var profile = new Profile
        {
            OutputDirectory = null,
            Crf = null,
            AudioBitrateKbps = null,
            AudioChannels = null,
            Resolution = null,
            MetadataBasedFilename = null,
            AudioLanguages = null,
            SubtitleLanguages = null,
        };

        // Act
        _ = context.Profiles.Add(profile);
        _ = await context.SaveChangesAsync();

        // Assert
        var storedProfile = await context.Profiles.FirstAsync();
        _ = storedProfile.OutputDirectory.Should().BeNull();
        _ = storedProfile.Crf.Should().BeNull();
        _ = storedProfile.AudioBitrateKbps.Should().BeNull();
        _ = storedProfile.AudioChannels.Should().BeNull();
        _ = storedProfile.Resolution.Should().BeNull();
        _ = storedProfile.MetadataBasedFilename.Should().BeNull();
        _ = storedProfile.AudioLanguages.Should().BeNull();
        _ = storedProfile.SubtitleLanguages.Should().BeNull();
    }
}
