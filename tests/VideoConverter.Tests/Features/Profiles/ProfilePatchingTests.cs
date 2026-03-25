using FluentAssertions;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Common.Models;
using Xunit;

namespace VideoConverter.Tests.Features.Profiles;

public class ProfilePatchingTests
{
    [Fact]
    public void Patch_ShouldUpdate_RequiredVmaf_On_PatchModel()
    {
        // Arrange
        var profile = new Profile
        {
            RequiredVmaf = null,
        };

        var patchModel = new ProfilePatchDto
        {
            RequiredVmaf = profile.RequiredVmaf,
        };

        var patchDoc = new JsonPatchDocument<ProfilePatchDto>();
        patchDoc.Operations.Add(new Operation<ProfilePatchDto>
        {
            op = "replace",
            path = "/RequiredVmaf",
            value = 92.0,
        });

        // Act
        patchDoc.ApplyTo(patchModel);

        // Assert
        patchModel.RequiredVmaf.Should().Be(92.0);
    }

    [Fact]
    public void Patch_ShouldUpdate_RequiredVmaf_With_Int_Value()
    {
        var profile = new Profile
        {
            RequiredVmaf = null,
        };

        var patchModel = new ProfilePatchDto
        {
            RequiredVmaf = profile.RequiredVmaf,
        };

        var patchDoc = new JsonPatchDocument<ProfilePatchDto>();
        patchDoc.Operations.Add(new Operation<ProfilePatchDto>
        {
            op = "replace",
            path = "/RequiredVmaf",
            value = 92,
        });

        patchDoc.ApplyTo(patchModel);

        patchModel.RequiredVmaf.Should().Be(92.0);
    }
}
