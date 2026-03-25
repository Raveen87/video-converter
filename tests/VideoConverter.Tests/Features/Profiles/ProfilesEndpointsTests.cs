using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using VideoConverter.Api.Features.Profiles.Api;
using VideoConverter.Api.Features.Profiles.Validation;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Infrastructure.Persistence;
using VideoConverter.Api.Shared.Validation;
using VideoConverter.Common.Validation;
using VideoConverter.Common.Validation.Requests;
using VideoConverter.Tests.TestUtils;

namespace VideoConverter.Tests.Features.Profiles;

public class ProfilesEndpointsTests
{
    [Fact]
    public async Task UpdateProfileAsync_WithRemoveRequiredVmaf_ClearsValue()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "vc-profile-update-" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempDir);

        try
        {
            await using var db = DbContextTestFactory.CreateInMemory();
            var profile = new Profile
            {
                Name = "Test",
                OutputDirectory = tempDir,
                RequiredVmaf = 92.0,
            };
            _ = db.Profiles.Add(profile);
            _ = await db.SaveChangesAsync();

            var patchDoc = new JsonPatchDocument<ProfilePatchDto>();
            patchDoc.Remove(x => x.RequiredVmaf);

            var endpoints = CreateEndpoints(db);
            _ = await endpoints.UpdateProfileAsync(profile.Id, patchDoc, CancellationToken.None);

            var updated = await db.Profiles.FindAsync(profile.Id);
            _ = updated.Should().NotBeNull();
            _ = updated.RequiredVmaf.Should().BeNull();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task UpdateProfileAsync_WithReplaceNullRequiredVmaf_ClearsValue()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "vc-profile-update-" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempDir);

        try
        {
            await using var db = DbContextTestFactory.CreateInMemory();
            var profile = new Profile
            {
                Name = "Test",
                OutputDirectory = tempDir,
                RequiredVmaf = 94.1,
            };
            _ = db.Profiles.Add(profile);
            _ = await db.SaveChangesAsync();

            var patchDoc = new JsonPatchDocument<ProfilePatchDto>();
            patchDoc.Replace(x => x.RequiredVmaf, (double?)null);

            var endpoints = CreateEndpoints(db);
            _ = await endpoints.UpdateProfileAsync(profile.Id, patchDoc, CancellationToken.None);

            var updated = await db.Profiles.FindAsync(profile.Id);
            _ = updated.Should().NotBeNull();
            _ = updated.RequiredVmaf.Should().BeNull();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task UpdateProfileAsync_WithoutRequiredVmafOperation_LeavesValueUnchanged()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "vc-profile-update-" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempDir);

        try
        {
            await using var db = DbContextTestFactory.CreateInMemory();
            var profile = new Profile
            {
                Name = "Test",
                OutputDirectory = tempDir,
                Crf = 22,
                RequiredVmaf = 90.6,
            };
            _ = db.Profiles.Add(profile);
            _ = await db.SaveChangesAsync();

            var patchDoc = new JsonPatchDocument<ProfilePatchDto>();
            patchDoc.Replace(x => x.Crf, 26);

            var endpoints = CreateEndpoints(db);
            _ = await endpoints.UpdateProfileAsync(profile.Id, patchDoc, CancellationToken.None);

            var updated = await db.Profiles.FindAsync(profile.Id);
            _ = updated.Should().NotBeNull();
            _ = updated.Crf.Should().Be(26);
            _ = updated.RequiredVmaf.Should().Be(90.6);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    private static ProfilesEndpoints CreateEndpoints(AppDbContext db)
    {
        var requestPropertyValidator = new RequestPropertyValidator();
        var fileSystemValidator = new FileSystemValidator();
        var createValidator = new ProfileCreateValidator(
            NullLogger<ProfileCreateValidator>.Instance,
            fileSystemValidator,
            requestPropertyValidator);
        var patchValidator = new ProfilePatchValidator(
            NullLogger<ProfilePatchValidator>.Instance,
            fileSystemValidator,
            requestPropertyValidator);
        var existsValidator = new ProfileExistsValidator(NullLogger<ProfileExistsValidator>.Instance);

        return new ProfilesEndpoints(
            db,
            NullLogger<ProfilesEndpoints>.Instance,
            createValidator,
            patchValidator,
            existsValidator);
    }
}
