using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Common.Models;
using VideoConverter.Tests.TestUtils;

namespace VideoConverter.Tests.Persistence;

/// <summary>
/// Property-based tests for OutputPathTemplate storage.
/// Feature: output-path-template, Property 1.
/// Validates: Requirements 10.1, 10.2.
/// </summary>
public class OutputPathTemplatePropertyTests
{
    /// <summary>
    /// Feature: output-path-template, Property 1: Template storage round-trip preserves original template.
    /// Validates: Requirements 10.1, 10.2.
    /// For any valid output path template string, storing it in a conversion or profile and then
    /// retrieving it SHALL return the exact same template string.
    /// </summary>
    /// <param name="template">The template string to test.</param>
    /// <returns>A property that validates the round-trip.</returns>
    [Property(MaxTest = 100)]
    public Property ConversionTemplateStorageRoundTrip_PreservesOriginalTemplate(NonNull<string> template)
    {
        using var context = DbContextTestFactory.CreateInMemory();

        var conversion = new Conversion
        {
            InputPath = "/input/test.mp4",
            OutputDirectory = "/output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            OutputPathTemplate = template.Item,
        };

        context.Conversions.Add(conversion);
        context.SaveChanges();

        var retrieved = context.Conversions.First(c => c.Id == conversion.Id);
        var matches = string.Equals(retrieved.OutputPathTemplate, template.Item, StringComparison.Ordinal);

        return matches.ToProperty().Label("Conversion template storage round-trip preserves original template");
    }

    /// <summary>
    /// Feature: output-path-template, Property 1: Template storage round-trip preserves original template.
    /// Validates: Requirements 10.1, 10.2.
    /// For any valid output path template string, storing it in a profile and then
    /// retrieving it SHALL return the exact same template string.
    /// </summary>
    /// <param name="template">The template string to test.</param>
    /// <returns>A property that validates the round-trip.</returns>
    [Property(MaxTest = 100)]
    public Property ProfileTemplateStorageRoundTrip_PreservesOriginalTemplate(NonNull<string> template)
    {
        using var context = DbContextTestFactory.CreateInMemory();

        var profile = new Profile
        {
            Name = "Test Profile",
            OutputPathTemplate = template.Item,
        };

        context.Profiles.Add(profile);
        context.SaveChanges();

        var retrieved = context.Profiles.First(p => p.Id == profile.Id);
        var matches = string.Equals(retrieved.OutputPathTemplate, template.Item, StringComparison.Ordinal);

        return matches.ToProperty().Label("Profile template storage round-trip preserves original template");
    }

    /// <summary>
    /// Feature: output-path-template, Property 1: Null template storage round-trip.
    /// Validates: Requirements 10.1, 10.2.
    /// When OutputPathTemplate is null, storing and retrieving SHALL preserve the null value.
    /// </summary>
    [Fact]
    public void NullTemplateStorageRoundTrip_PreservesNull()
    {
        using var context = DbContextTestFactory.CreateInMemory();

        var conversion = new Conversion
        {
            InputPath = "/input/test.mp4",
            OutputDirectory = "/output",
            CreatedAtUtc = DateTime.UtcNow,
            Status = ConversionStatus.Pending,
            OutputPathTemplate = null,
        };

        var profile = new Profile
        {
            Name = "Test Profile",
            OutputPathTemplate = null,
        };

        context.Conversions.Add(conversion);
        context.Profiles.Add(profile);
        context.SaveChanges();

        var retrievedConversion = context.Conversions.First(c => c.Id == conversion.Id);
        var retrievedProfile = context.Profiles.First(p => p.Id == profile.Id);

        Assert.Null(retrievedConversion.OutputPathTemplate);
        Assert.Null(retrievedProfile.OutputPathTemplate);
    }
}
